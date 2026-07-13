using FundedEdge.Domain.Enums;
using FundedEdge.Domain.Psychology;

namespace FundedEdge.Domain.Tests;

public class PsychDetectorEngineTests
{
    private static TradeWithEmotions Trade(
        DateTimeOffset openedAt,
        decimal netPnL,
        int quantity = 1,
        decimal? rMultiple = null,
        IReadOnlyList<EmotionRating>? entry = null,
        IReadOnlyList<EmotionRating>? exit = null,
        PlanAdherence adherence = PlanAdherence.FollowedPlan,
        bool wasImpulsive = false) => new(
            Guid.NewGuid(), openedAt, openedAt.AddMinutes(5), netPnL, rMultiple, quantity,
            entry ?? [], exit ?? [], adherence, wasImpulsive, null);

    [Fact]
    public void RevengeTrading_FastReentryAfterLossWithBiggerSize_Activates()
    {
        var t0 = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
        var trades = new List<TradeWithEmotions>
        {
            Trade(t0, -100m, quantity: 1),
            Trade(t0.AddMinutes(5), -50m, quantity: 2), // reentra a los 5 min tras perder, con tamaño mayor
        };

        var insight = PsychDetectorEngine.DetectRevengeTrading(trades);

        Assert.NotNull(insight);
        Assert.Equal("revenge-trading", insight!.DetectorKey);
    }

    [Fact]
    public void RevengeTrading_NormalSpacedTradesSameSize_DoesNotActivate()
    {
        var t0 = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
        var trades = new List<TradeWithEmotions>
        {
            Trade(t0, -100m, quantity: 1),
            Trade(t0.AddHours(2), 50m, quantity: 1),
        };

        var insight = PsychDetectorEngine.DetectRevengeTrading(trades);

        Assert.Null(insight);
    }

    [Fact]
    public void FomoConfirmado_FomoEntryImpulsiveNegativeR_Activates()
    {
        var t0 = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
        var trades = new List<TradeWithEmotions>
        {
            Trade(t0, -80m, rMultiple: -0.8m, entry: [new EmotionRating(EmotionType.Fomo, 4)], wasImpulsive: true),
        };

        var insight = PsychDetectorEngine.DetectFomoConfirmado(trades);

        Assert.NotNull(insight);
        Assert.Equal("fomo-confirmado", insight!.DetectorKey);
    }

    [Fact]
    public void FomoConfirmado_FomoEntryButProfitable_DoesNotActivate()
    {
        var t0 = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
        var trades = new List<TradeWithEmotions>
        {
            Trade(t0, 80m, rMultiple: 0.8m, entry: [new EmotionRating(EmotionType.Fomo, 4)], wasImpulsive: true),
        };

        var insight = PsychDetectorEngine.DetectFomoConfirmado(trades);

        Assert.Null(insight);
    }

    [Fact]
    public void IndisciplinaRentable_NoPlanButProfitable_Activates()
    {
        var t0 = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
        var trades = new List<TradeWithEmotions> { Trade(t0, 150m, adherence: PlanAdherence.NoPlan) };

        var insight = PsychDetectorEngine.DetectIndisciplinaRentable(trades);

        Assert.NotNull(insight);
        Assert.Equal("indisciplina-rentable", insight!.DetectorKey);
    }

    [Fact]
    public void IndisciplinaRentable_FollowedPlanAndProfitable_DoesNotActivate()
    {
        var t0 = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
        var trades = new List<TradeWithEmotions> { Trade(t0, 150m, adherence: PlanAdherence.FollowedPlan) };

        var insight = PsychDetectorEngine.DetectIndisciplinaRentable(trades);

        Assert.Null(insight);
    }

    [Fact]
    public void MalaRachaEmocional_TwoWeeksOfNegativeAndWorseningEmotions_Activates()
    {
        var start = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
        var trades = new List<TradeWithEmotions>();
        for (var day = 0; day < 14; day++)
        {
            // Valencia negativa y cada vez más intensa según avanza la racha.
            var intensity = 2 + day / 3;
            trades.Add(Trade(
                start.AddDays(day), -50m,
                entry: [new EmotionRating(EmotionType.Frustrated, Math.Min(intensity, 5))]));
        }

        var insight = PsychDetectorEngine.DetectMalaRachaEmocional(trades, []);

        Assert.NotNull(insight);
        Assert.Equal("mala-racha-emocional", insight!.DetectorKey);
    }

    [Fact]
    public void MalaRachaEmocional_TwoWeeksOfCalmPositiveTrading_DoesNotActivate()
    {
        var start = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
        var trades = new List<TradeWithEmotions>();
        for (var day = 0; day < 14; day++)
        {
            trades.Add(Trade(start.AddDays(day), 50m, entry: [new EmotionRating(EmotionType.Calm, 2)]));
        }

        var insight = PsychDetectorEngine.DetectMalaRachaEmocional(trades, []);

        Assert.Null(insight);
    }
}
