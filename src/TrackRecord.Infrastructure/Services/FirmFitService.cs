using Microsoft.EntityFrameworkCore;
using TrackRecord.Application.Abstractions;
using TrackRecord.Application.Dtos;
using TrackRecord.Domain.Entities;
using TrackRecord.Domain.Risk;
using TrackRecord.Infrastructure.Persistence;

namespace TrackRecord.Infrastructure.Services;

/// <summary>
/// Implementa el motor Firm Fit: para cada programa del catálogo, simula la operativa real del
/// usuario (distribución empírica de PnL por trade, agrupada en días según su cadencia real) contra
/// las reglas del programa y calcula probabilidad de pasar, EV, coste por cuenta fondeada, la
/// sensibilidad a cada regla y un Fit Score. Todo con los datos del usuario: nada genérico.
/// </summary>
public class FirmFitService(
    IDbContextFactory<TrackRecordDbContext> dbFactory,
    ICurrentUserAccessor currentUser,
    IPlanService planService) : IFirmFitService
{
    /// <summary>Por debajo de esta muestra de trades, el ranking se marca como poco fiable (solo orientativo).</summary>
    private const int MinTradesForConfidence = 20;

    public async Task<FirmFitRankingDto> RankProgramsAsync(CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var trades = await db.Trades.AsNoTracking()
            .Where(t => t.Account!.UserId == userId)
            .Select(t => new { Pnl = t.GrossPnL - t.Commissions, t.ClosedAt })
            .ToListAsync(ct);

        var pnls = trades.Select(t => t.Pnl).ToList();
        var distinctDays = trades.Select(t => t.ClosedAt.Date).Distinct().Count();
        var tradesPerDay = distinctDays > 0 ? Math.Max(1, (int)Math.Round((double)trades.Count / distinctDays)) : 1;

        // Ingreso medio por cuenta fondeada observado (incluye los 0 de fondeadas quemadas antes del
        // primer payout: parte real de la distribución). Sin fondeadas, no hay base para estimar el EV.
        var fundedPayouts = await db.TradingAccounts.AsNoTracking()
            .Where(a => a.UserId == userId && a.FundedOn != null)
            .Select(a => a.Payouts.Sum(p => (decimal?)p.AmountReceived) ?? 0m)
            .ToListAsync(ct);
        decimal? avgPayout = fundedPayouts.Count > 0 ? fundedPayouts.Average() : null;

        var programs = await db.EvaluationPrograms.AsNoTracking()
            .Where(p => p.IsActive)
            .Include(p => p.PropFirm)
            .ToListAsync(ct);

        var limits = await planService.GetLimitsAsync(ct: ct);

        var results = new List<FirmFitProgramDto>();
        if (pnls.Count > 0)
        {
            foreach (var program in programs)
            {
                results.Add(Evaluate(program, pnls, tradesPerDay, avgPayout));
            }

            // Primero los programas con EV estimable (mayor EV primero); el resto por P(pasar).
            results = results
                .OrderByDescending(r => r.EvPerEvaluation.HasValue)
                .ThenByDescending(r => r.EvPerEvaluation ?? decimal.MinValue)
                .ThenByDescending(r => r.PassProbability)
                .ToList();
        }

        var lowConfidence = pnls.Count < MinTradesForConfidence;
        var isLimited = !limits.FullRiskModule;
        if (isLimited && results.Count > 1)
        {
            results = results.Take(1).ToList();
        }

        return new FirmFitRankingDto(results, pnls.Count, tradesPerDay, lowConfidence, isLimited, avgPayout);
    }

    private static FirmFitProgramDto Evaluate(EvaluationProgram program, IReadOnlyList<decimal> pnls, int tradesPerDay, decimal? avgPayout)
    {
        var baseInput = ToInput(program, pnls, tradesPerDay);
        var fit = ProgramFitSimulator.Simulate(baseInput);
        var passProb = fit.ProbabilityOfPassing;

        decimal? ev = avgPayout is { } payout
            ? EvCalculator.ComputeEvPerEvaluation(passProb, payout, program.EvaluationCost, program.ActivationCost)
            : null;

        // Coste esperado para conseguir una fondeada: cada evaluación cuesta EvaluationCost y solo
        // una fracción P(pasar) llega a fondear (y solo entonces se paga la activación).
        decimal? costPerFunded = passProb > 0
            ? program.EvaluationCost / (decimal)passProb + program.ActivationCost
            : null;

        return new FirmFitProgramDto(
            ProgramId: program.Id,
            FirmName: program.PropFirm?.Name ?? "",
            ProgramName: program.Name,
            AccountSize: program.AccountSize,
            EvaluationCost: program.EvaluationCost,
            ActivationCost: program.ActivationCost,
            PassProbability: passProb,
            EvPerEvaluation: ev,
            CostPerFundedAccount: costPerFunded,
            AvgTradingDaysToPass: fit.AvgTradingDaysToPass,
            FitScore: ComputeFitScore(ev, program.EvaluationCost, passProb),
            RuleImpacts: ComputeRuleImpacts(program, baseInput, passProb));
    }

    private static ProgramFitInput ToInput(EvaluationProgram p, IReadOnlyList<decimal> pnls, int tradesPerDay) => new(
        TradePnLs: pnls,
        TradesPerDay: tradesPerDay,
        ProfitTarget: p.ProfitTarget,
        MaxDrawdown: p.MaxDrawdown,
        DrawdownType: p.DrawdownType,
        DailyLossLimit: p.DailyLossLimit,
        MinTradingDays: p.MinTradingDays,
        ConsistencyMaxDayFraction: p.ConsistencyMaxDayFraction);

    /// <summary>
    /// Sensibilidad por regla: re-simula quitando cada regla opcional presente y mide cuánto sube
    /// P(pasar) sin ella. El drawdown siempre existe, así que no se lista; se listan las reglas que
    /// de verdad diferencian a las firmas (pérdida diaria, consistencia, días mínimos).
    /// </summary>
    private static IReadOnlyList<RuleImpactDto> ComputeRuleImpacts(EvaluationProgram program, ProgramFitInput baseInput, double passProb)
    {
        var impacts = new List<RuleImpactDto>();

        if (program.DailyLossLimit is not null)
        {
            var without = ProgramFitSimulator.Simulate(baseInput with { DailyLossLimit = null }).ProbabilityOfPassing;
            impacts.Add(new RuleImpactDto("daily-loss", without, without - passProb));
        }

        if (program.ConsistencyMaxDayFraction is not null)
        {
            var without = ProgramFitSimulator.Simulate(baseInput with { ConsistencyMaxDayFraction = null }).ProbabilityOfPassing;
            impacts.Add(new RuleImpactDto("consistency", without, without - passProb));
        }

        if (program.MinTradingDays is not null)
        {
            var without = ProgramFitSimulator.Simulate(baseInput with { MinTradingDays = null }).ProbabilityOfPassing;
            impacts.Add(new RuleImpactDto("min-trading-days", without, without - passProb));
        }

        return impacts.OrderByDescending(i => i.Delta).ToList();
    }

    /// <summary>
    /// Fit Score 0-100. Con EV disponible: 50 = breakeven, saturando suavemente con el EV relativo
    /// al coste de la evaluación (tanh). Sin EV (sin payouts observados): proxy por probabilidad de
    /// pasar, con tope 60 para no aparentar una certeza económica que aún no tenemos.
    /// </summary>
    private static int ComputeFitScore(decimal? ev, decimal evaluationCost, double passProb)
    {
        if (ev is { } value && evaluationCost > 0m)
        {
            var score = 50.0 + 50.0 * Math.Tanh((double)value / (double)evaluationCost);
            return (int)Math.Round(Math.Clamp(score, 0, 100));
        }

        return (int)Math.Round(Math.Clamp(passProb * 60.0, 0, 60));
    }
}
