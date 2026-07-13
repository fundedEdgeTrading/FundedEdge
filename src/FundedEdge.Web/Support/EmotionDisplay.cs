using FundedEdge.Domain.Enums;

namespace FundedEdge.Web.Support;

/// <summary>Etiquetas y emoji de cada emoción para la UI (GUIA_PSICOLOGIA_TRADING.md §1.2). Presentación pura, sin lógica.</summary>
public static class EmotionDisplay
{
    private static readonly Dictionary<EmotionType, (string Label, string Emoji)> Map = new()
    {
        [EmotionType.Calm] = ("Calma", "😌"),
        [EmotionType.Confident] = ("Confianza", "💪"),
        [EmotionType.Euphoric] = ("Euforia", "🚀"),
        [EmotionType.Hopeful] = ("Esperanza", "🤞"),
        [EmotionType.Anxious] = ("Ansiedad", "😰"),
        [EmotionType.Fearful] = ("Miedo", "😨"),
        [EmotionType.Fomo] = ("FOMO", "🏃"),
        [EmotionType.Frustrated] = ("Frustración", "😤"),
        [EmotionType.Vengeful] = ("Venganza", "🔥"),
        [EmotionType.Bored] = ("Aburrimiento", "🥱"),
        [EmotionType.Doubtful] = ("Duda", "🤔"),
        [EmotionType.Regretful] = ("Arrepentimiento", "😞"),
        [EmotionType.Overconfident] = ("Exceso de confianza", "😎"),
        [EmotionType.Detached] = ("Desconexión", "🫥"),
    };

    public static string Label(EmotionType emotion) => Map[emotion].Label;
    public static string Emoji(EmotionType emotion) => Map[emotion].Emoji;
    public static IReadOnlyList<EmotionType> All => Map.Keys.ToList();
}
