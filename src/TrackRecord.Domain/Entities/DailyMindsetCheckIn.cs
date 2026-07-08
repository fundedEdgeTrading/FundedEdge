using TrackRecord.Domain.Common;
using TrackRecord.Domain.Enums;

namespace TrackRecord.Domain.Entities;

/// <summary>
/// Check-in diario de estado general (uno por usuario y día de operativa), GUIA_PSICOLOGIA_TRADING.md
/// §3. Sueño, estrés externo y foco pre-mercado explican a menudo más varianza en el resultado que
/// el propio trade.
/// </summary>
public class DailyMindsetCheckIn : Entity
{
    public string UserId { get; set; } = null!;
    public DateOnly Date { get; set; }

    /// <summary>1–5: calidad de sueño, estrés externo (vida/trabajo) y foco pre-mercado.</summary>
    public int SleepQuality { get; set; }
    public int ExternalStress { get; set; }
    public int PreMarketFocus { get; set; }

    public EmotionType DominantPreMarketEmotion { get; set; }
    public string? Note { get; set; }
}
