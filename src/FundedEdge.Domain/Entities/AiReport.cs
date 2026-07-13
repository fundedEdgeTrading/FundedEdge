using FundedEdge.Domain.Common;
using FundedEdge.Domain.Enums;

namespace FundedEdge.Domain.Entities;

/// <summary>
/// Informe o respuesta generada por Claude a partir de los KPIs agregados de la cuenta/negocio.
/// Se persiste para poder consultar la evolución de los diagnósticos a lo largo del tiempo
/// (ver GUIA_IMPLEMENTACION.md §9.5).
/// </summary>
public class AiReport : Entity
{
    /// <summary>Dueño del informe. Nullable por el mismo motivo que TradingAccount.UserId.</summary>
    public string? UserId { get; set; }

    public AiReportKind Kind { get; set; }

    /// <summary>Solo poblado cuando Kind = AdHocQuestion.</summary>
    public string? Question { get; set; }

    public string Content { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }

    public string Model { get; set; } = null!;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
}
