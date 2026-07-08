using TrackRecord.Application.Dtos;

namespace TrackRecord.Application.Abstractions;

/// <summary>
/// Semáforo de reglas en tiempo real (GUIA_FUNCIONALIDADES_PROPUESTAS.md §2.2/§3.5): cuánto
/// margen queda hoy frente a la pérdida diaria, el drawdown y la regla de consistencia de cada
/// cuenta activa, con las reglas reales de su programa de evaluación cuando está enlazado.
/// </summary>
public interface IRuleComplianceService
{
    Task<IReadOnlyList<AccountComplianceStatusDto>> GetComplianceStatusAsync(CancellationToken ct = default);
}
