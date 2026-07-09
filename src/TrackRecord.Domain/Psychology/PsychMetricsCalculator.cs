using TrackRecord.Domain.Enums;

namespace TrackRecord.Domain.Psychology;

/// <summary>Punto de la "equity curve emocional" (media móvil de valencia diaria).</summary>
public record EmotionalCapitalPoint(DateOnly Date, double Valence);

/// <summary>Métricas psicológicas derivadas (GUIA_PSICOLOGIA_TRADING.md §6.2).</summary>
public record PsychMetrics(
    int TiltIndex,
    int DisciplineScore,
    decimal? EmotionalCostPerR,
    IReadOnlyList<EmotionalCapitalPoint> EmotionalCapitalTrend);

public static class PsychMetricsCalculator
{
    public static PsychMetrics Compute(IReadOnlyList<TradeWithEmotions> trades, IReadOnlyList<DailyMindset> checkIns)
    {
        return new PsychMetrics(
            TiltIndex: ComputeTiltIndex(trades),
            DisciplineScore: ComputeDisciplineScore(trades),
            EmotionalCostPerR: ComputeEmotionalCostPerR(trades),
            EmotionalCapitalTrend: ComputeEmotionalCapitalTrend(trades, checkIns));
    }

    /// <summary>0-100: revenge trading (peso .40) + intensidad media de emociones de alta activación negativa (.35) + desviación de tamaño post-pérdida (.25).</summary>
    public static int ComputeTiltIndex(IReadOnlyList<TradeWithEmotions> trades)
    {
        if (trades.Count == 0) return 0;
        var ordered = trades.OrderBy(t => t.OpenedAt).ToList();

        var revengeEpisodes = (PsychDetectorEngine.DetectRevengeTrading(ordered)?.EvidenceTradeIds.Count ?? 0) / 2.0;
        var revengeComponent = Math.Clamp(revengeEpisodes / 5.0, 0, 1);

        var negativeRatings = ordered
            .SelectMany(t => t.EntryEmotions.Concat(t.ExitEmotions))
            .Where(e => EmotionProfile.IsHighActivationNegative(e.Emotion))
            .Select(e => e.Intensity)
            .ToList();
        var activationComponent = negativeRatings.Count > 0 ? negativeRatings.Average() / 5.0 : 0;

        var postLossCount = 0;
        var postLossSizeUp = 0;
        for (var i = 1; i < ordered.Count; i++)
        {
            if (!ordered[i - 1].IsLoss) continue;
            postLossCount++;
            if (ordered[i].Quantity > ordered[i - 1].Quantity) postLossSizeUp++;
        }
        var sizeDeviationComponent = postLossCount > 0 ? (double)postLossSizeUp / postLossCount : 0;

        var score = 100 * (0.40 * revengeComponent + 0.35 * activationComponent + 0.25 * sizeDeviationComponent);
        return (int)Math.Round(Math.Clamp(score, 0, 100));
    }

    /// <summary>% de trades con plan seguido, penalizado por la fracción de entradas impulsivas.</summary>
    public static int ComputeDisciplineScore(IReadOnlyList<TradeWithEmotions> trades)
    {
        if (trades.Count == 0) return 0;

        var followedFraction = trades.Count(t => t.Adherence == PlanAdherence.FollowedPlan) / (double)trades.Count;
        var impulsiveFraction = trades.Count(t => t.WasImpulsive) / (double)trades.Count;

        var score = 100 * followedFraction - 30 * impulsiveFraction;
        return (int)Math.Round(Math.Clamp(score, 0, 100));
    }

    /// <summary>Diferencia de expectancy (R) entre trades con entrada "Calma" y el resto — cuánto cuestan las emociones.</summary>
    public static decimal? ComputeEmotionalCostPerR(IReadOnlyList<TradeWithEmotions> trades)
    {
        var calmTrades = trades.Where(t => t.HasEntryEmotion(EmotionType.Calm) && t.RMultiple is not null).ToList();
        var restTrades = trades.Where(t => !t.HasEntryEmotion(EmotionType.Calm) && t.RMultiple is not null).ToList();

        if (calmTrades.Count == 0 || restTrades.Count == 0) return null;

        var calmAvg = calmTrades.Average(t => t.RMultiple!.Value);
        var restAvg = restTrades.Average(t => t.RMultiple!.Value);
        return calmAvg - restAvg;
    }

    /// <summary>Media móvil (7 días) de la valencia diaria — la "equity curve emocional".</summary>
    public static IReadOnlyList<EmotionalCapitalPoint> ComputeEmotionalCapitalTrend(
        IReadOnlyList<TradeWithEmotions> trades, IReadOnlyList<DailyMindset> checkIns)
    {
        var daily = PsychDetectorEngine.DailyValence(trades, checkIns).OrderBy(d => d.Date).ToList();
        if (daily.Count == 0) return [];

        var result = new List<EmotionalCapitalPoint>();
        var window = new Queue<double>();
        foreach (var (date, valence) in daily)
        {
            window.Enqueue(valence);
            if (window.Count > 7) window.Dequeue();
            result.Add(new EmotionalCapitalPoint(date, window.Average()));
        }
        return result;
    }
}
