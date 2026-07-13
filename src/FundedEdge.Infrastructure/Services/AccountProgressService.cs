using Microsoft.EntityFrameworkCore;
using FundedEdge.Application.Abstractions;
using FundedEdge.Application.Dtos;
using FundedEdge.Domain.Entities;
using FundedEdge.Domain.Enums;
using FundedEdge.Infrastructure.Persistence;

namespace FundedEdge.Infrastructure.Services;

public class AccountProgressService(
    IDbContextFactory<FundedEdgeDbContext> dbFactory,
    ICurrentUserAccessor currentUser,
    IRiskAnalysisService riskAnalysis) : IAccountProgressService
{
    public async Task<AccountProgressDto?> GetProgressAsync(Guid accountId, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var account = await db.TradingAccounts
            .AsNoTracking()
            .Include(a => a.PropFirm)
            .Include(a => a.EvaluationProgram)
            .Include(a => a.Trades)
            .Include(a => a.Payouts)
            .SingleOrDefaultAsync(a => a.Id == accountId && a.UserId == userId, ct);

        if (account is null || account.EvaluationProgramId is null || account.EvaluationProgram is null)
            return null;

        var program = account.EvaluationProgram;

        return account.Stage switch
        {
            AccountStage.Evaluation => new AccountProgressDto(
                account.Id,
                account.DisplayName,
                account.PropFirmId,
                account.PropFirm!.Name,
                program.Id,
                program.Name,
                account.Stage,
                await BuildEvaluationProgressAsync(account.Id, account, program, ct),
                null),

            AccountStage.Funded => new AccountProgressDto(
                account.Id,
                account.DisplayName,
                account.PropFirmId,
                account.PropFirm!.Name,
                program.Id,
                program.Name,
                account.Stage,
                null,
                BuildFundedProgress(account, program)),

            _ => null,
        };
    }

    // ── Fase Evaluación ─────────────────────────────────────────────────────────────────────

    private async Task<EvaluationProgressDto> BuildEvaluationProgressAsync(
        Guid accountId,
        TradingAccount account,
        EvaluationProgram program,
        CancellationToken ct)
    {
        var trades = account.Trades;
        var currentPnL = trades.Sum(t => t.GrossPnL - t.Commissions);
        var profitTargetPct = program.ProfitTarget > 0
            ? (double)(currentPnL / program.ProfitTarget)
            : 0;

        // Drawdown consumido: usamos la curva de equity para calcular el peor retroceso
        // desde el pico según el tipo de drawdown del programa.
        var drawdownConsumed = CalculateDrawdownConsumed(trades, program.DrawdownType, program.AccountSize);
        var drawdownConsumedPct = program.MaxDrawdown > 0
            ? (double)(drawdownConsumed / program.MaxDrawdown)
            : 0;

        // P&L del día de hoy
        var today = DateOnly.FromDateTime(DateTime.Today);
        var todayPnL = trades
            .Where(t => DateOnly.FromDateTime(t.ClosedAt.Date) == today)
            .Sum(t => t.GrossPnL - t.Commissions);

        // Mejor día (para regla de consistencia)
        var dailyPnLs = trades
            .GroupBy(t => DateOnly.FromDateTime(t.ClosedAt.Date))
            .Select(g => g.Sum(t => t.GrossPnL - t.Commissions))
            .ToList();

        var bestDayPnL = dailyPnLs.Count > 0 ? dailyPnLs.Max() : 0m;
        var bestDayFraction = currentPnL > 0 && dailyPnLs.Count > 0
            ? (double)(bestDayPnL / currentPnL)
            : 0;

        // Días de trading completados (días distintos con al menos un trade)
        var tradingDaysCompleted = trades
            .Select(t => DateOnly.FromDateTime(t.ClosedAt.Date))
            .Distinct()
            .Count();

        // Monte Carlo via IRiskAnalysisService
        double? passProbability = null;
        try
        {
            var riskResult = await riskAnalysis.RunAccountSimulationAsync(accountId, ct);
            passProbability = riskResult?.Simulation.ProbabilityOfReachingTarget;
        }
        catch
        {
            // Si no hay suficientes datos para simular, dejamos null.
        }

        return new EvaluationProgressDto(
            currentPnL,
            program.ProfitTarget,
            profitTargetPct,
            drawdownConsumed,
            program.MaxDrawdown,
            drawdownConsumedPct,
            program.DrawdownType,
            program.DailyLossLimit,
            todayPnL,
            program.ConsistencyMaxDayFraction,
            bestDayPnL,
            bestDayFraction,
            tradingDaysCompleted,
            program.MinTradingDays,
            passProbability);
    }

    // ── Fase Fondeada ───────────────────────────────────────────────────────────────────────

    private static FundedProgressDto BuildFundedProgress(
        TradingAccount account,
        EvaluationProgram program)
    {
        var fundedOn = account.FundedOn;
        var tradesAfterFunding = fundedOn.HasValue
            ? account.Trades.Where(t => DateOnly.FromDateTime(t.ClosedAt.Date) >= fundedOn.Value).ToList()
            : account.Trades.ToList();

        var grossProfit = tradesAfterFunding.Sum(t => t.GrossPnL - t.Commissions);
        var totalPayoutsRequested = account.Payouts.Sum(p => p.AmountRequested);
        var netProfit = grossProfit - totalPayoutsRequested;

        // Drawdown: usamos las reglas de la fase fondeada si están definidas, si no las de evaluación.
        var fundedDrawdownType = program.FundedDrawdownType ?? program.DrawdownType;
        var fundedMaxDrawdown  = program.FundedMaxDrawdown  ?? program.MaxDrawdown;

        var drawdownConsumed = CalculateDrawdownConsumed(tradesAfterFunding, fundedDrawdownType, account.AccountSize);
        var drawdownBufferRemaining = Math.Max(0m, fundedMaxDrawdown - drawdownConsumed);

        // Cálculo del retiro máximo:
        //   cap_aplicable = si hay PayoutMaxProfitPct: min(netProfit × cap, drawdown_buffer)
        //                   si no: min(netProfit, drawdown_buffer)
        //   retiro_bruto  = max(0, cap_aplicable)
        //   retiro_neto   = retiro_bruto × PayoutSplitTraderPct
        var capApplicable = program.PayoutMaxProfitPct.HasValue
            ? netProfit * program.PayoutMaxProfitPct.Value
            : netProfit;
        var maxWithdrawalGross = Math.Max(0m, Math.Min(capApplicable, drawdownBufferRemaining));
        var maxWithdrawalNet   = maxWithdrawalGross * program.PayoutSplitTraderPct;

        // Elegibilidad de payout
        var fundedTradingDays = tradesAfterFunding
            .Select(t => DateOnly.FromDateTime(t.ClosedAt.Date))
            .Distinct()
            .Count();

        var minDaysOk = program.FundedMinTradingDays is null || fundedTradingDays >= program.FundedMinTradingDays.Value;

        // Mejor día para la regla de consistencia (fase fondeada)
        var fundedBestDayPnL = tradesAfterFunding
            .GroupBy(t => DateOnly.FromDateTime(t.ClosedAt.Date))
            .Select(g => g.Sum(t => t.GrossPnL - t.Commissions))
            .DefaultIfEmpty(0m)
            .Max();

        DateOnly? nextPayoutEligibleOn = null;
        if (program.PayoutMinDaysBetween.HasValue)
        {
            var lastPayoutOn = account.Payouts.Count > 0
                ? account.Payouts.Max(p => p.RequestedOn)
                : (DateOnly?)null;
            var baseDate = lastPayoutOn ?? fundedOn;
            nextPayoutEligibleOn = baseDate?.AddDays(program.PayoutMinDaysBetween.Value);
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var daysOk = nextPayoutEligibleOn is null || nextPayoutEligibleOn.Value <= today;
        var isPayoutEligible = minDaysOk && daysOk && netProfit > 0;

        // Si ya es elegible, no mostramos la fecha futura.
        if (isPayoutEligible) nextPayoutEligibleOn = null;

        return new FundedProgressDto(
            netProfit,
            totalPayoutsRequested,
            maxWithdrawalGross,
            maxWithdrawalNet,
            program.PayoutSplitTraderPct,
            program.PayoutMaxProfitPct,
            drawdownBufferRemaining,
            fundedMaxDrawdown,
            fundedDrawdownType,
            isPayoutEligible,
            isPayoutEligible ? null : nextPayoutEligibleOn,
            fundedTradingDays,
            program.FundedMinTradingDays,
            program.ConsistencyMaxDayFraction,
            fundedBestDayPnL);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────────────

    private static decimal CalculateDrawdownConsumed(
        IEnumerable<Trade> trades,
        DrawdownType drawdownType,
        decimal accountSize)
    {
        var orderedTrades = trades.OrderBy(t => t.ClosedAt).ToList();
        if (orderedTrades.Count == 0) return 0m;

        switch (drawdownType)
        {
            case DrawdownType.Trailing:
            {
                // Trailing: el suelo sube con el pico de equity. Drawdown consumido = pico - equity_actual.
                var equity = accountSize;
                var peak   = accountSize;
                foreach (var t in orderedTrades)
                {
                    equity += t.GrossPnL - t.Commissions;
                    if (equity > peak) peak = equity;
                }
                return Math.Max(0m, peak - equity);
            }
            case DrawdownType.EndOfDay:
            {
                // EOD: igual que trailing pero el pico solo se actualiza al cierre de cada día.
                // El consumido es el retroceso actual respecto al pico de cierre, no el peor histórico:
                // si cierras el día en máximos, el suelo sube con él y el consumido se resetea a 0.
                var equity = accountSize;
                var peak   = accountSize;
                var dailyGroups = orderedTrades
                    .GroupBy(t => t.ClosedAt.Date)
                    .OrderBy(g => g.Key);
                foreach (var day in dailyGroups)
                {
                    var dayPnL = day.Sum(t => t.GrossPnL - t.Commissions);
                    equity += dayPnL;
                    if (equity > peak) peak = equity;
                }
                return Math.Max(0m, peak - equity);
            }
            default: // Static
            {
                // Static: el suelo es fijo en accountSize - maxDrawdown. Drawdown consumido = pérdida desde el inicio.
                var totalPnL = orderedTrades.Sum(t => t.GrossPnL - t.Commissions);
                return Math.Max(0m, -totalPnL);
            }
        }
    }
}
