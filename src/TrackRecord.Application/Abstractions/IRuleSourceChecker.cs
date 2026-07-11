using TrackRecord.Application.Dtos;

namespace TrackRecord.Application.Abstractions;

/// <summary>
/// Comprueba una fuente de reglas: descarga la página, normaliza el contenido, lo hashea y lo
/// compara con el hash de la comprobación anterior. Si cambió, actualiza la fuente, lanza la
/// extracción LLM (si hay API key) y notifica. Lo usan tanto el job diario
/// (RuleSourceMonitorService) como los botones "Comprobar ahora"/"Extraer ahora" del admin.
/// </summary>
public interface IRuleSourceChecker
{
    /// <param name="forceExtraction">
    /// Ejecuta la extracción aunque el hash no haya cambiado (útil tras dar de alta una fuente,
    /// cuya primera comprobación solo fija la línea base).
    /// </param>
    Task<RuleSourceCheckResult> CheckAsync(Guid ruleSourceId, bool forceExtraction = false, CancellationToken ct = default);
}
