using FundedEdge.Domain.Enums;

namespace FundedEdge.Application.Ai;

/// <summary>
/// Análisis de la operativa y del negocio de fondeo asistido por IA (Claude), a partir de los
/// KPIs agregados calculados por IKpiService. Ver GUIA_IMPLEMENTACION.md §9.
/// </summary>
public interface ITradingAnalystService
{
    /// <summary>Genera y persiste un informe completo: fortalezas, fugas, viabilidad y plan de acción.</summary>
    Task<AiReportDto> GenerateAnalysisReportAsync(CancellationToken ct = default);

    /// <summary>Responde una pregunta puntual del usuario usando las estadísticas actuales como contexto.</summary>
    Task<AiReportDto> AskQuestionAsync(string question, CancellationToken ct = default);

    /// <summary>Informe centrado en el patrón emocional del trader (diario emocional + detectores), GUIA_PSICOLOGIA_TRADING.md §8.2.</summary>
    Task<AiReportDto> GeneratePsychologyReportAsync(CancellationToken ct = default);

    /// <summary>
    /// Mini-informe disparado por un evento (racha de pérdidas, riesgo de drawdown, primer payout),
    /// GUIA_FUNCIONALIDADES_PROPUESTAS.md §4.1. <paramref name="eventContext"/> describe el disparador
    /// concreto (p.ej. la cuenta y sus números) para que el informe sea específico, no genérico.
    /// </summary>
    Task<AiReportDto> GenerateEventReportAsync(AiReportKind eventKind, string eventContext, CancellationToken ct = default);

    /// <summary>
    /// Informe de inspiración sobre la operativa de otro trader Elite del ranking (F5.6),
    /// identificado por el slug de su página pública. Requiere que el usuario actual sea Elite y
    /// que el dueño del perfil haya dado opt-in de compartir su operativa. El informe se persiste
    /// a nombre del usuario que lo pide y consume su cupo de IA.
    /// </summary>
    Task<AiReportDto> GeneratePeerInspirationReportAsync(string peerSlug, CancellationToken ct = default);

    Task<IReadOnlyList<AiReportDto>> GetHistoryAsync(int take = 20, CancellationToken ct = default);

    /// <summary>Indica si hay una API key de Anthropic configurada (env var o appsettings), sin exponerla.</summary>
    bool IsConfigured { get; }
}
