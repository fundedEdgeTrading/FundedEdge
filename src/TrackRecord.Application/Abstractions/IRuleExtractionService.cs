namespace TrackRecord.Application.Abstractions;

/// <summary>
/// Extracción LLM de reglas de programas desde el texto de una página oficial (fase 2 de
/// INVESTIGACION_AUTOMATIZACION_REGLAS.md). Compara lo extraído con el catálogo activo de la
/// firma y crea propuestas en staging (ProposedProgramChange); nunca escribe el catálogo.
/// </summary>
public interface IRuleExtractionService
{
    /// <summary>Hay credenciales de Anthropic configuradas para poder extraer.</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Extrae los programas del texto normalizado de la página de una fuente y crea (o refresca)
    /// propuestas pendientes para los que difieren del catálogo activo.
    /// Devuelve el número de propuestas creadas o actualizadas.
    /// </summary>
    Task<int> ProposeFromContentAsync(Guid ruleSourceId, string pageText, CancellationToken ct = default);
}
