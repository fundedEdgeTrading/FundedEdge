namespace FundedEdge.Application.Abstractions;

/// <summary>
/// Extrae, mediante IA, un JSON estructurado de programas de evaluación a partir del contenido
/// (Markdown/HTML) del reglamento de una firma. El JSON producido sigue el mismo esquema que
/// consume <see cref="IExternalFirmDataProvider"/>, por lo que la validación se reutiliza sin
/// duplicar lógica (DRY / Open-Closed: cambiar el modelo de IA no afecta al consumidor).
/// </summary>
public interface IRuleExtractionService
{
    /// <summary>True si hay API key de IA configurada.</summary>
    bool IsConfigured { get; }

    /// <summary>Devuelve un array JSON de programas extraído del <paramref name="rulesContent"/>.</summary>
    Task<string> ExtractProgramsJsonAsync(string firmName, string rulesContent, CancellationToken ct = default);
}
