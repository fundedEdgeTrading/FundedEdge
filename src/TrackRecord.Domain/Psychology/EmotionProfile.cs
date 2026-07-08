using TrackRecord.Domain.Enums;

namespace TrackRecord.Domain.Psychology;

public enum ActivationLevel { Low, Medium, High }

/// <summary>
/// Mapea cada <see cref="EmotionType"/> sobre el modelo circumplejo (valencia × activación),
/// GUIA_PSICOLOGIA_TRADING.md §1.2. Permite agregar/graficar aunque el usuario elija emociones
/// distintas para fenómenos similares.
/// </summary>
public static class EmotionProfile
{
    private static readonly Dictionary<EmotionType, (int Valence, ActivationLevel Activation)> Map = new()
    {
        [EmotionType.Calm] = (1, ActivationLevel.Low),
        [EmotionType.Confident] = (1, ActivationLevel.Medium),
        [EmotionType.Euphoric] = (1, ActivationLevel.High),
        [EmotionType.Hopeful] = (1, ActivationLevel.Medium),
        [EmotionType.Anxious] = (-1, ActivationLevel.High),
        [EmotionType.Fearful] = (-1, ActivationLevel.High),
        [EmotionType.Fomo] = (-1, ActivationLevel.High),
        [EmotionType.Frustrated] = (-1, ActivationLevel.High),
        [EmotionType.Vengeful] = (-1, ActivationLevel.High),
        [EmotionType.Bored] = (-1, ActivationLevel.Low),
        [EmotionType.Doubtful] = (-1, ActivationLevel.Medium),
        [EmotionType.Regretful] = (-1, ActivationLevel.Medium),
        [EmotionType.Overconfident] = (1, ActivationLevel.High),
        [EmotionType.Detached] = (-1, ActivationLevel.Low),
    };

    public static int Valence(EmotionType emotion) => Map[emotion].Valence;
    public static ActivationLevel Activation(EmotionType emotion) => Map[emotion].Activation;

    /// <summary>Emociones de alta activación y valencia negativa: el combustible del tilt.</summary>
    public static bool IsHighActivationNegative(EmotionType emotion) =>
        Map[emotion] is (-1, ActivationLevel.High);
}
