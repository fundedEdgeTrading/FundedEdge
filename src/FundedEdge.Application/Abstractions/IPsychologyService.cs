using FundedEdge.Application.Dtos;

namespace FundedEdge.Application.Abstractions;

/// <summary>
/// Diario emocional por trade, check-in diario, analítica emociones×trades y motor de
/// diagnóstico determinista (GUIA_PSICOLOGIA_TRADING.md). Todos los datos son propios del
/// usuario actual (ver ICurrentUserAccessor) y nunca se exponen en PublicProfile ni en exports.
/// </summary>
public interface IPsychologyService
{
    /// <summary>Trades del usuario (día o rango) que aún no tienen registro emocional.</summary>
    Task<IReadOnlyList<PendingEmotionTradeDto>> GetPendingAsync(DateOnly from, DateOnly to, CancellationToken ct = default);

    Task SaveTradeEmotionsAsync(SaveTradeEmotionsRequest request, CancellationToken ct = default);

    Task<DailyCheckInDto?> GetDailyCheckInAsync(DateOnly date, CancellationToken ct = default);
    Task SaveDailyCheckInAsync(DailyCheckInDto dto, CancellationToken ct = default);

    /// <summary>Serie temporal y agregados para las gráficas de la página de psicología.</summary>
    Task<EmotionAnalyticsDto> GetAnalyticsAsync(DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>Métricas psicológicas derivadas (tilt, disciplina, coste emocional) e insights activos.</summary>
    Task<PsychMetricsDto> GetMetricsAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
}
