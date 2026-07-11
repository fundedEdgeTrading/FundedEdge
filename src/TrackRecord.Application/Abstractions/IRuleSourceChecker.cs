using TrackRecord.Application.Dtos;

namespace TrackRecord.Application.Abstractions;

/// <summary>
/// Comprueba una fuente de reglas: descarga la página, normaliza el contenido, lo hashea y lo
/// compara con el hash de la comprobación anterior. Si cambió, actualiza la fuente y notifica.
/// Lo usan tanto el job diario (RuleSourceMonitorService) como el botón "Comprobar ahora" del admin.
/// </summary>
public interface IRuleSourceChecker
{
    Task<RuleSourceCheckResult> CheckAsync(Guid ruleSourceId, CancellationToken ct = default);
}
