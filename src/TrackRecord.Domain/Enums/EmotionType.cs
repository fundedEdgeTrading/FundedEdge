namespace TrackRecord.Domain.Enums;

/// <summary>
/// Taxonomía cerrada de emociones de trading (GUIA_PSICOLOGIA_TRADING.md §1.2), mapeada sobre el
/// modelo circumplejo (valencia × activación) vía <see cref="Psychology.EmotionProfile"/>.
/// </summary>
public enum EmotionType
{
    Calm = 0,
    Confident = 1,
    Euphoric = 2,
    Hopeful = 3,

    Anxious = 10,
    Fearful = 11,
    Fomo = 12,
    Frustrated = 13,
    Vengeful = 14,

    Bored = 20,
    Doubtful = 21,
    Regretful = 22,
    Overconfident = 23,
    Detached = 24,
}
