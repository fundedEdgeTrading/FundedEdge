using FundedEdge.Application.Dtos;

namespace FundedEdge.Application.Abstractions;

public interface IRoadmapService
{
    /// <summary>
    /// Prioriza cuentas fondeadas (saldo disponible para retirar) y evaluaciones (cercanía al
    /// objetivo), y sugiere el reparto de los payouts históricos entre reinversión, uso personal
    /// y provisión fiscal.
    /// </summary>
    Task<RoadmapDto> GetRoadmapAsync(
        decimal reinvestPercent = 0.4m,
        decimal personalPercent = 0.4m,
        decimal taxPercent = 0.2m,
        CancellationToken ct = default);
}
