namespace TrackRecord.Application.Ai;

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

    Task<IReadOnlyList<AiReportDto>> GetHistoryAsync(int take = 20, CancellationToken ct = default);

    /// <summary>Indica si hay una API key de Anthropic configurada (env var o appsettings), sin exponerla.</summary>
    bool IsConfigured { get; }
}
