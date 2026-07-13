using FundedEdge.Domain.Common;
using FundedEdge.Domain.Enums;

namespace FundedEdge.Domain.Entities;

public class PropFirm : Entity
{
    public string Name { get; set; } = null!;
    public string? Website { get; set; }
    public string? Notes { get; set; }

    // ── Monitor de salud (M6, capa editorial) ───────────────────────────────────────────────────

    /// <summary>Estado editorial de la firma. Un cambio avisa a los usuarios con cuentas activas.</summary>
    public FirmHealthStatus HealthStatus { get; set; } = FirmHealthStatus.Active;

    /// <summary>País/jurisdicción de la firma (texto libre, p.ej. "EE. UU.").</summary>
    public string? Country { get; set; }

    /// <summary>Notas editoriales de salud: cambios de reglas recientes, retrasos reportados…</summary>
    public string? HealthNotes { get; set; }

    /// <summary>Última revisión editorial del estado/notas de salud.</summary>
    public DateOnly? HealthUpdatedOn { get; set; }

    /// <summary>
    /// Días mínimos entre solicitudes de payout de esta firma (p.ej. 14). Null = regla no
    /// configurada — no se calcula cuenta atrás para sus cuentas. Alimenta
    /// TradingAccountDetailDto.NextPayoutEligibleOn.
    /// </summary>
    public int? MinDaysBetweenPayouts { get; set; }

    public List<TradingAccount> Accounts { get; set; } = [];

    /// <summary>
    /// Programas de evaluación de esta firma. Incluye los inactivos (versionados); filtrar por
    /// <see cref="EvaluationProgram.IsActive"/> para obtener solo el catálogo vigente.
    /// </summary>
    public List<EvaluationProgram> Programs { get; set; } = [];
}
