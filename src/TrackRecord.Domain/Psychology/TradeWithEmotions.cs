using TrackRecord.Domain.Enums;

namespace TrackRecord.Domain.Psychology;

/// <summary>Una emoción con intensidad, tal y como se registra en el formulario para un momento dado.</summary>
public record EmotionRating(EmotionType Emotion, int Intensity);

/// <summary>
/// Vista aplanada de un trade + su diario emocional, usada como entrada pura de los detectores y
/// del cálculo de métricas (GUIA_PSICOLOGIA_TRADING.md §6). No depende de EF ni de ningún estado
/// externo, para que los detectores sean testables con datos sintéticos.
/// </summary>
public record TradeWithEmotions(
    Guid TradeId,
    DateTimeOffset OpenedAt,
    DateTimeOffset ClosedAt,
    decimal NetPnL,
    decimal? RMultiple,
    int Quantity,
    IReadOnlyList<EmotionRating> EntryEmotions,
    IReadOnlyList<EmotionRating> ExitEmotions,
    PlanAdherence Adherence,
    bool WasImpulsive,
    string? Note)
{
    public TimeSpan Duration => ClosedAt - OpenedAt;
    public bool IsWin => NetPnL > 0;
    public bool IsLoss => NetPnL < 0;
    public bool HasEmotion(EmotionType emotion) => EntryEmotions.Any(e => e.Emotion == emotion) || ExitEmotions.Any(e => e.Emotion == emotion);
    public bool HasEntryEmotion(EmotionType emotion) => EntryEmotions.Any(e => e.Emotion == emotion);
}

/// <summary>Check-in diario aplanado para los detectores (GUIA_PSICOLOGIA_TRADING.md §3).</summary>
public record DailyMindset(
    DateOnly Date,
    int SleepQuality,
    int ExternalStress,
    int PreMarketFocus,
    EmotionType DominantPreMarketEmotion);
