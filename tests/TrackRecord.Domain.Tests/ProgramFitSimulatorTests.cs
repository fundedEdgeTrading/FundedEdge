using TrackRecord.Domain.Enums;
using TrackRecord.Domain.Risk;

namespace TrackRecord.Domain.Tests;

public class ProgramFitSimulatorTests
{
    private static ProgramFitInput BaseInput(
        IReadOnlyList<decimal> pnls,
        decimal? dailyLossLimit = null,
        int? minTradingDays = null,
        decimal? consistency = null,
        int tradesPerDay = 1,
        decimal profitTarget = 3_000m,
        decimal maxDrawdown = 2_000m) => new(
            TradePnLs: pnls,
            TradesPerDay: tradesPerDay,
            ProfitTarget: profitTarget,
            MaxDrawdown: maxDrawdown,
            DrawdownType: DrawdownType.Trailing,
            DailyLossLimit: dailyLossLimit,
            MinTradingDays: minTradingDays,
            ConsistencyMaxDayFraction: consistency,
            Iterations: 5_000);

    [Fact]
    public void Simulate_OnlyWinningTrades_NoRules_AlwaysPasses()
    {
        var result = ProgramFitSimulator.Simulate(BaseInput([100m, 250m, 80m]));

        Assert.Equal(1.0, result.ProbabilityOfPassing);
        Assert.Equal(0.0, result.ProbabilityOfBusting);
        Assert.NotNull(result.AvgTradingDaysToPass);
    }

    [Fact]
    public void Simulate_OnlyLosingTrades_AlwaysBusts()
    {
        var result = ProgramFitSimulator.Simulate(BaseInput([-100m, -50m]));

        Assert.Equal(1.0, result.ProbabilityOfBusting);
        Assert.Equal(0.0, result.ProbabilityOfPassing);
        Assert.Null(result.AvgTradingDaysToPass);
    }

    [Fact]
    public void Simulate_DailyLossLimit_IncreasesBustProbability()
    {
        // Días de 3 trades volátiles: un tope de pérdida diaria solo puede quemar más caminos,
        // nunca menos, que la misma operativa sin tope.
        var pnls = new[] { 220m, -190m, 180m, -160m, 200m, -210m };

        var withoutLimit = ProgramFitSimulator.Simulate(BaseInput(pnls, tradesPerDay: 3));
        var withLimit = ProgramFitSimulator.Simulate(BaseInput(pnls, dailyLossLimit: 300m, tradesPerDay: 3));

        Assert.True(withLimit.ProbabilityOfBusting >= withoutLimit.ProbabilityOfBusting,
            $"con tope diario bust={withLimit.ProbabilityOfBusting} debería ser ≥ sin tope {withoutLimit.ProbabilityOfBusting}");
    }

    [Fact]
    public void Simulate_MinTradingDays_ForcesAtLeastThatManyDays()
    {
        // Trades idénticos ganadores ⇒ camino determinista: sin mínimo pasa en 3 días (100×3=300);
        // con mínimo de 5, no puede pasar antes del día 5.
        var noMinimum = ProgramFitSimulator.Simulate(BaseInput([100m], profitTarget: 300m));
        var withMinimum = ProgramFitSimulator.Simulate(BaseInput([100m], minTradingDays: 5, profitTarget: 300m));

        Assert.Equal(3.0, noMinimum.AvgTradingDaysToPass);
        Assert.Equal(5.0, withMinimum.AvgTradingDaysToPass);
    }

    [Fact]
    public void Simulate_ConsistencyRule_DelaysOrPreventsPassing()
    {
        // La regla de consistencia nunca acelera el pase: los días medios hasta pasar con la regla
        // deben ser >= que sin ella (o no pasar en absoluto).
        var pnls = new[] { 100m };

        var without = ProgramFitSimulator.Simulate(BaseInput(pnls, profitTarget: 300m));
        var with = ProgramFitSimulator.Simulate(BaseInput(pnls, consistency: 0.30m, profitTarget: 300m));

        Assert.NotNull(without.AvgTradingDaysToPass);
        Assert.NotNull(with.AvgTradingDaysToPass);
        Assert.True(with.AvgTradingDaysToPass >= without.AvgTradingDaysToPass,
            $"con consistencia {with.AvgTradingDaysToPass} debería tardar ≥ que sin ella {without.AvgTradingDaysToPass}");
    }

    [Fact]
    public void Simulate_SameSeed_IsDeterministic()
    {
        var input = BaseInput([120m, -90m, 60m], dailyLossLimit: 250m, tradesPerDay: 2);

        Assert.Equal(ProgramFitSimulator.Simulate(input, seed: 5), ProgramFitSimulator.Simulate(input, seed: 5));
    }

    [Fact]
    public void Simulate_ProbabilitiesSumToOne()
    {
        var result = ProgramFitSimulator.Simulate(BaseInput([300m, -250m, 150m, -200m, 400m, -350m], tradesPerDay: 2));

        var total = result.ProbabilityOfPassing + result.ProbabilityOfBusting + result.ProbabilityOfTimeout;
        Assert.Equal(1.0, total, precision: 10);
    }

    [Fact]
    public void Simulate_NoTrades_Throws()
    {
        var input = BaseInput([]);

        Assert.Throws<ArgumentException>(() => ProgramFitSimulator.Simulate(input));
    }

    [Fact]
    public void Simulate_TradesPerDayBelowOne_Throws()
    {
        var input = BaseInput([100m]) with { TradesPerDay = 0 };

        Assert.Throws<ArgumentOutOfRangeException>(() => ProgramFitSimulator.Simulate(input));
    }
}
