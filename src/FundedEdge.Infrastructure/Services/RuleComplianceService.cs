using Microsoft.EntityFrameworkCore;
using FundedEdge.Application.Abstractions;
using FundedEdge.Application.Dtos;
using FundedEdge.Domain.Enums;
using FundedEdge.Infrastructure.Persistence;

namespace FundedEdge.Infrastructure.Services;

/// <summary>
/// Calcula, para cada cuenta activa, cuánto margen queda hoy frente a las reglas de su programa
/// (GUIA_FUNCIONALIDADES_PROPUESTAS.md §2.2/§3.5). Usa las reglas del programa vigente para la
/// etapa actual (fondeada vs evaluación) si la cuenta está enlazada a uno; si no, cae a los
/// campos propios de la cuenta (MaxDrawdown/DrawdownType) y no evalúa pérdida diaria/consistencia.
/// </summary>
public class RuleComplianceService(
    IDbContextFactory<FundedEdgeDbContext> dbFactory,
    ICurrentUserAccessor currentUser) : IRuleComplianceService
{
    private const double YellowThreshold = 0.5;
    private const double RedThreshold = 0.8;

    public async Task<IReadOnlyList<AccountComplianceStatusDto>> GetComplianceStatusAsync(CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var accounts = await db.TradingAccounts.AsNoTracking()
            .Where(a => a.UserId == userId)
            .Where(a => a.Stage == AccountStage.Evaluation || a.Stage == AccountStage.Funded)
            .Select(a => new
            {
                a.Id,
                a.DisplayName,
                a.Stage,
                a.MaxDrawdown,
                a.DrawdownType,
                ProgramDailyLossLimit = a.EvaluationProgram == null ? (decimal?)null : a.EvaluationProgram.DailyLossLimit,
                ProgramFundedDailyLossLimit = a.EvaluationProgram == null ? (decimal?)null : a.EvaluationProgram.FundedDailyLossLimit,
                ProgramMaxDrawdown = a.EvaluationProgram == null ? (decimal?)null : (decimal?)a.EvaluationProgram.MaxDrawdown,
                ProgramFundedMaxDrawdown = a.EvaluationProgram == null ? (decimal?)null : a.EvaluationProgram.FundedMaxDrawdown,
                ProgramDrawdownType = a.EvaluationProgram == null ? (DrawdownType?)null : (DrawdownType?)a.EvaluationProgram.DrawdownType,
                ProgramFundedDrawdownType = a.EvaluationProgram == null ? (DrawdownType?)null : a.EvaluationProgram.FundedDrawdownType,
                ConsistencyMaxDayFraction = a.EvaluationProgram == null ? (decimal?)null : a.EvaluationProgram.ConsistencyMaxDayFraction,
            })
            .ToListAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var result = new List<AccountComplianceStatusDto>();

        foreach (var account in accounts)
        {
            var isFunded = account.Stage == AccountStage.Funded;
            decimal? dailyLossLimit = isFunded ? account.ProgramFundedDailyLossLimit ?? account.ProgramDailyLossLimit : account.ProgramDailyLossLimit;
            var maxDrawdown = (isFunded ? account.ProgramFundedMaxDrawdown : account.ProgramMaxDrawdown) ?? account.MaxDrawdown;
            var drawdownType = (isFunded ? account.ProgramFundedDrawdownType : account.ProgramDrawdownType) ?? account.DrawdownType;

            var trades = await db.Trades.AsNoTracking()
                .Where(t => t.AccountId == account.Id)
                .OrderBy(t => t.ClosedAt)
                .Select(t => new { t.ClosedAt, NetPnL = t.GrossPnL - t.Commissions })
                .ToListAsync(ct);

            // ── Drawdown: mismo algoritmo que IRiskAnalysisService.GetDrawdownAlertsAsync ──
            decimal equity = 0m, peak = 0m;
            foreach (var t in trades)
            {
                equity += t.NetPnL;
                if (drawdownType != DrawdownType.Static)
                {
                    peak = Math.Max(peak, equity);
                }
            }
            var floor = peak - maxDrawdown;
            var drawdownRemaining = Math.Max(equity - floor, 0m);
            var drawdownConsumed = maxDrawdown > 0 ? Math.Clamp(1 - (double)(drawdownRemaining / maxDrawdown), 0, 1) : 0;
            var drawdownLevel = LevelFor(drawdownConsumed);

            // ── Pérdida diaria: PnL de hoy frente al límite del programa ──
            var dailyLossToday = Math.Max(-trades.Where(t => DateOnly.FromDateTime(t.ClosedAt.Date) == today).Sum(t => t.NetPnL), 0m);
            decimal? dailyLossRemaining = dailyLossLimit is > 0 ? dailyLossLimit - dailyLossToday : null;
            var dailyLossLevel = dailyLossLimit is > 0
                ? LevelFor(Math.Clamp((double)(dailyLossToday / dailyLossLimit.Value), 0, 1))
                : ComplianceLevel.Green;

            // ── Consistencia: fracción del profit total aportada por el mejor día ──
            ComplianceLevel? consistencyLevel = null;
            double? topDayFraction = null;
            if (account.ConsistencyMaxDayFraction is > 0)
            {
                var dailyProfits = trades
                    .GroupBy(t => DateOnly.FromDateTime(t.ClosedAt.Date))
                    .Select(g => g.Sum(t => t.NetPnL))
                    .Where(p => p > 0)
                    .ToList();
                var totalProfit = dailyProfits.Sum();
                if (totalProfit > 0)
                {
                    topDayFraction = (double)(dailyProfits.Max() / totalProfit);
                    var ratio = topDayFraction.Value / (double)account.ConsistencyMaxDayFraction.Value;
                    consistencyLevel = LevelFor(Math.Clamp(ratio, 0, 1));
                }
            }

            var overall = new[] { dailyLossLevel, drawdownLevel, consistencyLevel ?? ComplianceLevel.Green }.Max();

            result.Add(new AccountComplianceStatusDto(
                account.Id, account.DisplayName,
                dailyLossLimit, dailyLossToday, dailyLossRemaining, dailyLossLevel,
                maxDrawdown, drawdownRemaining, drawdownLevel,
                account.ConsistencyMaxDayFraction, topDayFraction, consistencyLevel,
                overall));
        }

        return result;
    }

    private static ComplianceLevel LevelFor(double consumedFraction) => consumedFraction switch
    {
        >= RedThreshold => ComplianceLevel.Red,
        >= YellowThreshold => ComplianceLevel.Yellow,
        _ => ComplianceLevel.Green,
    };
}
