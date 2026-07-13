using Microsoft.EntityFrameworkCore;
using FundedEdge.Application.Abstractions;
using FundedEdge.Application.Dtos;
using FundedEdge.Domain.Enums;
using FundedEdge.Domain.Risk;
using FundedEdge.Infrastructure.Persistence;

namespace FundedEdge.Infrastructure.Services;

public class RiskAnalysisService(IDbContextFactory<FundedEdgeDbContext> dbFactory, ICurrentUserAccessor currentUser) : IRiskAnalysisService
{
    /// <summary>
    /// Mínimo de trades propios para simular una cuenta con su propia distribución; por debajo se
    /// muestrea la distribución global de todos los trades (mismo trader, más muestra).
    /// </summary>
    private const int MinOwnTradesForAccountSim = 20;

    public async Task<RiskDefaultsDto> GetDefaultsAsync(CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var funnel = await LoadFunnelAsync(db, userId, ct);

        var tradesAvailable = await db.Trades.CountAsync(t => t.Account!.UserId == userId, ct);

        double? passRate = funnel.Terminated.Count > 0
            ? (double)funnel.Terminated.Count(a => a.EverFunded) / funnel.Terminated.Count
            : null;

        var ev = funnel.Terminated.Count > 0 ? EvCalculator.Estimate(funnel.Outcomes) : null;

        double? kelly = passRate is not null && funnel.AvgEvaluationCost is > 0 && funnel.PayoutsPerFunded.Count > 0
            ? EvCalculator.KellyFraction(
                passRate.Value, funnel.PayoutsPerFunded.Average(), funnel.AvgEvaluationCost.Value, funnel.AvgActivationCost ?? 0m)
            : null;

        return new RiskDefaultsDto(
            PassRate: passRate,
            AvgEvaluationCost: funnel.AvgEvaluationCost,
            AvgActivationCost: funnel.AvgActivationCost,
            PayoutsPerFundedAccount: funnel.PayoutsPerFunded,
            EvaluationsTerminated: funnel.Terminated.Count,
            FundedAccounts: funnel.Terminated.Count(a => a.EverFunded),
            TradesAvailable: tradesAvailable,
            Ev: ev,
            KellyFraction: kelly);
    }

    public async Task<BankrollPlanResult> RunBankrollPlanAsync(BankrollPlanRequest request, CancellationToken ct = default)
    {
        var defaults = await GetDefaultsAsync(ct);

        var passRate = request.PassRateOverride ?? defaults.PassRate
            ?? throw new InvalidOperationException(
                "No hay evaluaciones terminadas de las que derivar un pass rate. Introduce uno manualmente.");
        var evaluationCost = request.EvaluationCostOverride ?? defaults.AvgEvaluationCost
            ?? throw new InvalidOperationException(
                "No hay costes de evaluación registrados. Introduce un coste por evaluación manualmente.");
        var activationCost = request.ActivationCostOverride ?? defaults.AvgActivationCost ?? 0m;

        var input = new RuinSimulationInput(
            Bankroll: request.Bankroll,
            EvaluationCost: evaluationCost,
            ActivationCost: activationCost,
            PassRate: passRate,
            HistoricalPayoutsPerFundedAccount: defaults.PayoutsPerFundedAccount,
            MonthlyEvaluationBudget: request.MonthlyEvaluationBudget,
            Months: request.Months,
            Iterations: Math.Clamp(request.Iterations, 1_000, 50_000));

        var simulation = BankrollSimulator.Simulate(input);
        var minimumBankroll = BankrollSimulator.FindMinimumBankroll(input);

        return new BankrollPlanResult(simulation, minimumBankroll, input);
    }

    public async Task<AccountRiskResultDto?> RunAccountSimulationAsync(Guid accountId, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var account = await db.TradingAccounts.AsNoTracking()
            .Where(a => a.Id == accountId && a.UserId == userId)
            .Select(a => new { a.DisplayName, a.ProfitTarget, a.MaxDrawdown, a.DrawdownType })
            .SingleOrDefaultAsync(ct);
        if (account is null || account.ProfitTarget <= 0 || account.MaxDrawdown <= 0)
        {
            return null;
        }

        var ownPnls = await db.Trades.AsNoTracking()
            .Where(t => t.AccountId == accountId)
            .Select(t => t.GrossPnL - t.Commissions)
            .ToListAsync(ct);

        var usedGlobal = ownPnls.Count < MinOwnTradesForAccountSim;
        var pnls = usedGlobal
            ? await db.Trades.AsNoTracking().Where(t => t.Account!.UserId == userId).Select(t => t.GrossPnL - t.Commissions).ToListAsync(ct)
            : ownPnls;
        if (pnls.Count == 0)
        {
            return null;
        }

        var simulation = AccountSimulator.Simulate(new AccountSimulationInput(
            pnls, account.ProfitTarget, account.MaxDrawdown, account.DrawdownType));

        return new AccountRiskResultDto(accountId, account.DisplayName, simulation, pnls.Count, usedGlobal);
    }

    private const double DrawdownAlertThreshold = 0.8;

    public async Task<IReadOnlyList<DrawdownAlertDto>> GetDrawdownAlertsAsync(CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var accounts = await db.TradingAccounts.AsNoTracking()
            .Where(a => a.UserId == userId)
            .Where(a => a.Stage == AccountStage.Evaluation || a.Stage == AccountStage.Funded)
            .Where(a => a.MaxDrawdown > 0)
            .Select(a => new { a.Id, a.DisplayName, a.MaxDrawdown, a.DrawdownType })
            .ToListAsync(ct);

        var alerts = new List<DrawdownAlertDto>();
        foreach (var account in accounts)
        {
            var pnls = await db.Trades.AsNoTracking()
                .Where(t => t.AccountId == account.Id)
                .OrderBy(t => t.ClosedAt)
                .Select(t => t.GrossPnL - t.Commissions)
                .ToListAsync(ct);
            if (pnls.Count == 0) continue;

            decimal equity = 0m, peak = 0m;
            foreach (var pnl in pnls)
            {
                equity += pnl;
                if (account.DrawdownType != DrawdownType.Static)
                {
                    peak = Math.Max(peak, equity);
                }
            }

            var floor = peak - account.MaxDrawdown;
            var remainingBuffer = equity - floor;
            var consumedFraction = Math.Clamp(1 - (double)(remainingBuffer / account.MaxDrawdown), 0, 1);

            if (consumedFraction >= DrawdownAlertThreshold)
            {
                alerts.Add(new DrawdownAlertDto(account.Id, account.DisplayName, consumedFraction, Math.Max(remainingBuffer, 0m)));
            }
        }

        return alerts;
    }

    private sealed record TerminatedAccount(bool EverFunded, decimal EvaluationCosts, decimal ActivationCosts, decimal Payouts);

    private sealed record FunnelData(
        IReadOnlyList<TerminatedAccount> Terminated,
        IReadOnlyList<EvaluationOutcome> Outcomes,
        IReadOnlyList<decimal> PayoutsPerFunded,
        decimal? AvgEvaluationCost,
        decimal? AvgActivationCost);

    /// <summary>
    /// Evaluaciones terminadas = alguna vez fondeadas, o falladas/expiradas sin llegar a fondeo.
    /// (Withdrawn sin fondear no es resultado de evaluación; mismo criterio que KpiService.)
    /// </summary>
    private static async Task<FunnelData> LoadFunnelAsync(FundedEdgeDbContext db, string userId, CancellationToken ct)
    {
        var accounts = await db.TradingAccounts.AsNoTracking()
            .Where(a => a.UserId == userId)
            .Where(a => a.FundedOn != null || a.Stage == AccountStage.Failed || a.Stage == AccountStage.Expired)
            .Select(a => new TerminatedAccount(
                a.FundedOn != null,
                a.Costs.Where(c => c.Kind == CostKind.Evaluation || c.Kind == CostKind.Reset).Sum(c => (decimal?)c.Amount) ?? 0m,
                a.Costs.Where(c => c.Kind == CostKind.Activation).Sum(c => (decimal?)c.Amount) ?? 0m,
                a.Payouts.Sum(p => (decimal?)p.AmountReceived) ?? 0m))
            .ToListAsync(ct);

        var outcomes = accounts
            .Select(a => new EvaluationOutcome(a.EverFunded, a.Payouts - a.EvaluationCosts - a.ActivationCosts))
            .ToList();

        // Incluye los 0 de cuentas fondeadas quemadas antes del primer payout: son parte real de la distribución.
        var payoutsPerFunded = accounts.Where(a => a.EverFunded).Select(a => a.Payouts).ToList();

        var withEvalCost = accounts.Where(a => a.EvaluationCosts > 0).ToList();
        var fundedWithActivation = accounts.Where(a => a.EverFunded && a.ActivationCosts > 0).ToList();

        return new FunnelData(
            accounts,
            outcomes,
            payoutsPerFunded,
            AvgEvaluationCost: withEvalCost.Count > 0 ? withEvalCost.Average(a => a.EvaluationCosts) : null,
            AvgActivationCost: fundedWithActivation.Count > 0 ? fundedWithActivation.Average(a => a.ActivationCosts) : null);
    }
}
