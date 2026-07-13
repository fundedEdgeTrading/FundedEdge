using FundedEdge.Domain.Common;
using FundedEdge.Domain.Enums;

namespace FundedEdge.Domain.Entities;

/// <summary>
/// Registro emocional de un trade (GUIA_PSICOLOGIA_TRADING.md §3). Varias filas por trade: una por
/// cada <see cref="EmotionMoment"/> registrado (antes/durante/después), y hasta 3 emociones por
/// momento (multi-select en el formulario). Adherencia al plan e impulsividad se repiten en las
/// filas del mismo trade — son propiedades del trade, no del momento emocional concreto.
/// Separado de <see cref="Trade"/>: el trade importado por webhook no se toca, el diario llega después.
/// Nunca se expone en <see cref="PublicProfile"/> ni en exports.
/// </summary>
public class TradeEmotionLog : Entity
{
    public Guid TradeId { get; set; }
    public Trade? Trade { get; set; }

    public EmotionMoment Moment { get; set; }
    public EmotionType Emotion { get; set; }

    /// <summary>Intensidad auto-reportada, 1–5.</summary>
    public int Intensity { get; set; }

    public PlanAdherence Adherence { get; set; }

    /// <summary>Auto-reporte: "¿fue una entrada impulsiva?"</summary>
    public bool WasImpulsive { get; set; }

    /// <summary>Nota libre opcional (máx. 500 chars). Nunca obligatoria.</summary>
    public string? Note { get; set; }

    public DateTimeOffset LoggedAt { get; set; }
}
