using FundedEdge.Application.Dtos;

namespace FundedEdge.Application.Abstractions;

/// <summary>
/// Motor Firm Fit (feature diferenciadora, ver GUIA_FEATURE_DIFERENCIADORA.md): cruza la
/// distribución real de PnL por trade del usuario con las reglas de cada programa de evaluación del
/// catálogo para responder "¿qué evaluación me conviene comprar?". Prescriptivo, no descriptivo:
/// ranking por EV/probabilidad de pasar con TUS datos, no comparación genérica de precios.
/// </summary>
public interface IFirmFitService
{
    /// <summary>
    /// Rankea los programas de evaluación activos para el usuario actual. Si su plan no incluye el
    /// módulo completo (Starter), el ranking se limita al mejor programa (con IsLimitedByPlan=true).
    /// </summary>
    Task<FirmFitRankingDto> RankProgramsAsync(CancellationToken ct = default);
}
