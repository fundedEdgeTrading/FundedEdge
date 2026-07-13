using FundedEdge.Domain.Enums;
using FundedEdge.Domain.Risk;

namespace FundedEdge.Domain.Tests;

public class BankrollSimulatorTests
{
    private static RuinSimulationInput BaseInput(decimal bankroll = 2_000m, double passRate = 0.4) => new(
        Bankroll: bankroll,
        EvaluationCost: 150m,
        ActivationCost: 100m,
        PassRate: passRate,
        HistoricalPayoutsPerFundedAccount: [1_000m, 2_000m, 500m],
        MonthlyEvaluationBudget: 4,
        Months: 12,
        Iterations: 5_000);

    [Fact]
    public void Simulate_PassRateOneWithBigPayouts_RuinIsNearZero()
    {
        var result = BankrollSimulator.Simulate(BaseInput(passRate: 1.0));

        Assert.True(result.ProbabilityOfRuin < 0.01, $"P(ruina)={result.ProbabilityOfRuin} debería ser ≈0");
        Assert.True(result.MedianFinalBankroll > 2_000m, "con EV muy positivo, la mediana debe crecer");
    }

    [Fact]
    public void Simulate_PassRateZero_LongHorizon_RuinIsCertain()
    {
        var result = BankrollSimulator.Simulate(BaseInput(passRate: 0.0) with { Months = 120 });

        Assert.Equal(1.0, result.ProbabilityOfRuin);
        Assert.Null(result.MedianMonthsToBreakeven); // nunca se recupera el capital inicial
    }

    [Fact]
    public void Simulate_SameSeed_IsDeterministic()
    {
        var a = BankrollSimulator.Simulate(BaseInput(), seed: 7);
        var b = BankrollSimulator.Simulate(BaseInput(), seed: 7);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Simulate_PercentilesAreOrdered()
    {
        var result = BankrollSimulator.Simulate(BaseInput());

        Assert.True(result.P5FinalBankroll <= result.MedianFinalBankroll);
        Assert.True(result.MedianFinalBankroll <= result.P95FinalBankroll);
    }

    [Fact]
    public void Simulate_BankrollBelowEvaluationCost_IsImmediateRuin()
    {
        var result = BankrollSimulator.Simulate(BaseInput(bankroll: 100m)); // < 150 de coste

        Assert.Equal(1.0, result.ProbabilityOfRuin);
    }

    [Fact]
    public void FindMinimumBankroll_PositiveEdge_ReturnsAffordableFigure()
    {
        var input = BaseInput(passRate: 0.5) with { Iterations = 2_000 };

        var minimum = BankrollSimulator.FindMinimumBankroll(input, maxRuinProbability: 0.05);

        Assert.NotNull(minimum);
        Assert.True(minimum >= input.EvaluationCost);
        // Verifica la garantía que promete la búsqueda: a ese bankroll, P(ruina) ≤ 5 %.
        var check = BankrollSimulator.Simulate(input with { Bankroll = minimum.Value });
        Assert.True(check.ProbabilityOfRuin <= 0.06, $"P(ruina)={check.ProbabilityOfRuin} en el mínimo recomendado");
    }

    [Fact]
    public void FindMinimumBankroll_NegativeEdge_ReturnsFullRunwayCapital()
    {
        // Pass rate 0: el proceso es determinista (se compra hasta agotar) y el "mínimo" es el
        // capital que financia la sangría de todo el horizonte: ~600 €/mes × 120 meses ≈ 72.000 €.
        // No mide viabilidad — eso lo comunica el semáforo de EV en la página /risk.
        var input = BaseInput(passRate: 0.0) with { Months = 120, Iterations = 500 };

        var minimum = BankrollSimulator.FindMinimumBankroll(input);

        Assert.NotNull(minimum);
        Assert.InRange(minimum.Value, 71_000m, 72_100m);
    }
}

public class AccountSimulatorTests
{
    [Fact]
    public void Simulate_OnlyWinningTrades_AlwaysReachesTarget()
    {
        var input = new AccountSimulationInput(
            TradePnLs: [100m, 250m, 80m],
            ProfitTarget: 3_000m,
            MaxDrawdown: 2_000m,
            DrawdownType: DrawdownType.Trailing,
            Iterations: 2_000);

        var result = AccountSimulator.Simulate(input);

        Assert.Equal(1.0, result.ProbabilityOfReachingTarget);
        Assert.Equal(0.0, result.ProbabilityOfBusting);
        Assert.NotNull(result.AvgTradesToTarget);
        Assert.Null(result.AvgTradesToBust);
    }

    [Fact]
    public void Simulate_OnlyLosingTrades_AlwaysBusts()
    {
        var input = new AccountSimulationInput(
            TradePnLs: [-100m, -50m],
            ProfitTarget: 3_000m,
            MaxDrawdown: 2_000m,
            DrawdownType: DrawdownType.Trailing,
            Iterations: 2_000);

        var result = AccountSimulator.Simulate(input);

        Assert.Equal(1.0, result.ProbabilityOfBusting);
        Assert.Equal(0.0, result.ProbabilityOfReachingTarget);
    }

    [Fact]
    public void Simulate_TrailingIsHarsherThanStatic()
    {
        // Con drawdown trailing el suelo sube con el equity: la misma operativa debe quemar
        // la cuenta con probabilidad mayor o igual que con drawdown estático.
        var pnls = new[] { 300m, -250m, 150m, -200m, 400m, -350m };

        var trailing = AccountSimulator.Simulate(new AccountSimulationInput(
            pnls, ProfitTarget: 3_000m, MaxDrawdown: 1_000m, DrawdownType.Trailing, Iterations: 5_000));
        var @static = AccountSimulator.Simulate(new AccountSimulationInput(
            pnls, ProfitTarget: 3_000m, MaxDrawdown: 1_000m, DrawdownType.Static, Iterations: 5_000));

        Assert.True(trailing.ProbabilityOfBusting >= @static.ProbabilityOfBusting);
    }

    [Fact]
    public void Simulate_SameSeed_IsDeterministic()
    {
        var input = new AccountSimulationInput(
            [100m, -80m, 60m], ProfitTarget: 1_000m, MaxDrawdown: 500m, DrawdownType.Trailing, Iterations: 1_000);

        Assert.Equal(AccountSimulator.Simulate(input, seed: 3), AccountSimulator.Simulate(input, seed: 3));
    }

    [Fact]
    public void Simulate_NoTrades_Throws()
    {
        var input = new AccountSimulationInput([], 1_000m, 500m, DrawdownType.Trailing);

        Assert.Throws<ArgumentException>(() => AccountSimulator.Simulate(input));
    }
}

public class EvCalculatorTests
{
    [Fact]
    public void ComputeEvPerEvaluation_MatchesFormula()
    {
        // EV = 0.4 × 1000 − 150 − 0.4 × 100 = 400 − 150 − 40 = 210
        var ev = EvCalculator.ComputeEvPerEvaluation(0.4, 1_000m, 150m, 100m);

        Assert.Equal(210m, ev);
    }

    [Fact]
    public void Estimate_PointEstimateIsSampleMean_AndCiContainsIt()
    {
        var outcomes = new List<EvaluationOutcome>
        {
            new(true, 750m),   // fondeada: 1000 payout − 150 eval − 100 activación
            new(false, -150m),
            new(false, -150m),
            new(true, 1_750m),
            new(false, -150m),
        };

        var estimate = EvCalculator.Estimate(outcomes);

        Assert.Equal(outcomes.Average(o => o.NetResult), estimate.EvPerEvaluation);
        Assert.Equal(5, estimate.SampleSize);
        Assert.NotNull(estimate.CiLower);
        Assert.NotNull(estimate.CiUpper);
        Assert.True(estimate.CiLower <= estimate.EvPerEvaluation);
        Assert.True(estimate.CiUpper >= estimate.EvPerEvaluation);
    }

    [Fact]
    public void Estimate_EmptySample_ReturnsZeroWithoutCi()
    {
        var estimate = EvCalculator.Estimate([]);

        Assert.Equal(0m, estimate.EvPerEvaluation);
        Assert.Equal(0, estimate.SampleSize);
        Assert.Null(estimate.CiLower);
    }

    [Fact]
    public void KellyFraction_MatchesBinaryFormula()
    {
        // b = (1000 − 100) / 150 = 6; f* = 0.4 − 0.6/6 = 0.3
        var kelly = EvCalculator.KellyFraction(0.4, 1_000m, 150m, 100m);

        Assert.NotNull(kelly);
        Assert.Equal(0.3, kelly.Value, precision: 10);
    }

    [Fact]
    public void KellyFraction_NoEdge_ReturnsNull()
    {
        // b = (100 − 100) / 150 = 0 → sin edge
        Assert.Null(EvCalculator.KellyFraction(0.4, 100m, 150m, 100m));
        // f* negativo (pass rate ínfimo) → null
        Assert.Null(EvCalculator.KellyFraction(0.01, 200m, 150m, 100m));
        // pass rate 0 → null
        Assert.Null(EvCalculator.KellyFraction(0.0, 1_000m, 150m, 100m));
    }
}
