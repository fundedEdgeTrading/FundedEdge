using FundedEdge.Domain.Enums;
using FundedEdge.Domain.Risk;

namespace FundedEdge.Domain.Tests;

public class ComplianceRuleEngineTests
{
    private static ComplianceTrade Trade(DateTime closedAt, decimal netPnL) => new(closedAt, netPnL);

    // ── Drawdown ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void EvaluateDrawdown_Trailing_FollowsHistoricalPeak()
    {
        // Pico en +1000 (día 2), luego retrocede a +700 (día 3): drawdown máximo 1000 →
        // suelo = 1000-1000 = 0, equity actual 700 → colchón restante 700.
        var trades = new List<ComplianceTrade>
        {
            Trade(new DateTime(2026, 1, 1), 500m),
            Trade(new DateTime(2026, 1, 2), 500m),
            Trade(new DateTime(2026, 1, 3), -300m),
        };

        var result = ComplianceRuleEngine.EvaluateDrawdown(trades, maxDrawdown: 1_000m, DrawdownType.Trailing);

        Assert.Equal(700m, result.RemainingBuffer);
        Assert.Equal(0.30, result.ConsumedFraction, 2);
    }

    [Fact]
    public void EvaluateDrawdown_Static_AnchorsFloorToInitialBalance_IgnoresPeak()
    {
        // Static: el suelo no sigue el pico (peak se queda en 0), solo el balance inicial.
        var trades = new List<ComplianceTrade>
        {
            Trade(new DateTime(2026, 1, 1), 1_000m),
            Trade(new DateTime(2026, 1, 2), -600m),
        };

        var result = ComplianceRuleEngine.EvaluateDrawdown(trades, maxDrawdown: 1_000m, DrawdownType.Static);

        // equity final = 400; suelo = 0 - 1000 = -1000; remaining = 400 - (-1000) = 1400.
        Assert.Equal(1_400m, result.RemainingBuffer);
        Assert.Equal(0.0, result.ConsumedFraction);
    }

    [Fact]
    public void EvaluateDrawdown_BreachBeyondFloor_RemainingClampedToZero_NotNegative()
    {
        var trades = new List<ComplianceTrade> { Trade(new DateTime(2026, 1, 1), -1_500m) };

        var result = ComplianceRuleEngine.EvaluateDrawdown(trades, maxDrawdown: 1_000m, DrawdownType.Trailing);

        Assert.Equal(0m, result.RemainingBuffer);
        Assert.Equal(1.0, result.ConsumedFraction);
    }

    // ── Daily loss ───────────────────────────────────────────────────────────────────────────

    [Fact]
    public void EvaluateDailyLoss_NoLimitConfigured_UsedTodayStillComputed_RemainingAndFractionNull()
    {
        var today = new DateOnly(2026, 1, 5);
        var trades = new List<ComplianceTrade> { Trade(today.ToDateTime(TimeOnly.MinValue), -200m) };

        var result = ComplianceRuleEngine.EvaluateDailyLoss(trades, dailyLossLimit: null, today);

        Assert.Equal(200m, result.UsedToday); // se informa igualmente aunque no haya límite que aplicar
        Assert.Null(result.Remaining);
        Assert.Null(result.ConsumedFraction);
    }

    [Fact]
    public void EvaluateDailyLoss_WithLimit_ComputesRemainingAndFraction_IgnoresOtherDays()
    {
        var today = new DateOnly(2026, 1, 5);
        var trades = new List<ComplianceTrade>
        {
            Trade(today.AddDays(-1).ToDateTime(TimeOnly.MinValue), -900m), // ayer: no cuenta
            Trade(today.ToDateTime(TimeOnly.MinValue), -300m),
            Trade(today.ToDateTime(new TimeOnly(14, 0)), 50m), // hoy, neto -250
        };

        var result = ComplianceRuleEngine.EvaluateDailyLoss(trades, dailyLossLimit: 1_000m, today);

        Assert.Equal(250m, result.UsedToday);
        Assert.Equal(750m, result.Remaining);
        Assert.Equal(0.25, result.ConsumedFraction);
    }

    [Fact]
    public void EvaluateDailyLoss_BreachBeyondLimit_RemainingGoesNegative_NotClamped()
    {
        var today = new DateOnly(2026, 1, 5);
        var trades = new List<ComplianceTrade> { Trade(today.ToDateTime(TimeOnly.MinValue), -1_200m) };

        var result = ComplianceRuleEngine.EvaluateDailyLoss(trades, dailyLossLimit: 1_000m, today);

        Assert.Equal(-200m, result.Remaining); // a diferencia del drawdown, esto NO se clampa a 0
        Assert.Equal(1.0, result.ConsumedFraction);
    }

    // ── Consistency ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void EvaluateConsistency_NoRuleConfigured_ReturnsNull()
    {
        var trades = new List<ComplianceTrade> { Trade(new DateTime(2026, 1, 1), 500m) };

        Assert.Null(ComplianceRuleEngine.EvaluateConsistency(trades, maxDayFraction: null));
    }

    [Fact]
    public void EvaluateConsistency_NoProfitableDaysYet_ReturnsNull_NotZero()
    {
        var trades = new List<ComplianceTrade> { Trade(new DateTime(2026, 1, 1), -500m) };

        Assert.Null(ComplianceRuleEngine.EvaluateConsistency(trades, maxDayFraction: 0.30m));
    }

    [Fact]
    public void EvaluateConsistency_TopDayFraction_IsRawAndUnclamped_EvenWhenRuleBreached()
    {
        // Dos días rentables: +700 y +300 (total 1000). El mejor día aporta 70% del profit total,
        // muy por encima del límite del 30%. topDayFraction debe reflejar el 70% real (sin clampar),
        // aunque ConsumedFraction sí se clampe a 1.0 para el semáforo.
        var trades = new List<ComplianceTrade>
        {
            Trade(new DateTime(2026, 1, 1), 700m),
            Trade(new DateTime(2026, 1, 2), 300m),
        };

        var result = ComplianceRuleEngine.EvaluateConsistency(trades, maxDayFraction: 0.30m);

        Assert.NotNull(result);
        Assert.Equal(0.70, result!.TopDayFraction, 2);
        Assert.Equal(1.0, result.ConsumedFraction);
    }

    [Fact]
    public void EvaluateConsistency_WithinLimit_ComputesProportionalFraction()
    {
        // 3 días rentables iguales (+100 cada uno): el mejor día es 1/3 ≈ 0.333 del total.
        // Límite 0.50 → ConsumedFraction = 0.333/0.50 ≈ 0.667.
        var trades = new List<ComplianceTrade>
        {
            Trade(new DateTime(2026, 1, 1), 100m),
            Trade(new DateTime(2026, 1, 2), 100m),
            Trade(new DateTime(2026, 1, 3), 100m),
        };

        var result = ComplianceRuleEngine.EvaluateConsistency(trades, maxDayFraction: 0.50m);

        Assert.NotNull(result);
        Assert.Equal(1.0 / 3.0, result!.TopDayFraction, 3);
        Assert.Equal(0.667, result.ConsumedFraction, 3);
    }

    [Fact]
    public void EvaluateConsistency_LosingDaysExcludedFromTotalProfit()
    {
        // Día 1: +1000 (rentable). Día 2: -400 (pérdida, se excluye del total de profit).
        // totalProfit = 1000 (solo días positivos) → topDayFraction = 1000/1000 = 1.0.
        var trades = new List<ComplianceTrade>
        {
            Trade(new DateTime(2026, 1, 1), 1_000m),
            Trade(new DateTime(2026, 1, 2), -400m),
        };

        var result = ComplianceRuleEngine.EvaluateConsistency(trades, maxDayFraction: 0.30m);

        Assert.NotNull(result);
        Assert.Equal(1.0, result!.TopDayFraction, 2);
    }
}
