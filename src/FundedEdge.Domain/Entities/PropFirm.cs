using FundedEdge.Domain.Common;

namespace FundedEdge.Domain.Entities;

public class PropFirm : Entity
{
    public string Name { get; set; } = null!;
    public string? Website { get; set; }
    public string? Notes { get; set; }

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
