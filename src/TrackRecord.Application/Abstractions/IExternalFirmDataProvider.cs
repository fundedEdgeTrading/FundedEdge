using TrackRecord.Application.Dtos;

namespace TrackRecord.Application.Abstractions;

/// <summary>
/// Proveedor externo de datos de programas de evaluación. Interfaz stub para futura ingesta
/// automatizada (scraping, IA extrayendo HTML, importación de JSON estructurado).
/// La implementación actual (<c>ManualExternalFirmDataProvider</c>) parsea un string JSON
/// subido por el usuario desde la UI.
/// </summary>
public interface IExternalFirmDataProvider
{
    /// <summary>
    /// Parsea un JSON estructurado (array de programas) y devuelve la lista de DTOs listos para
    /// ser revisados y guardados por el usuario. No persiste nada — solo extrae y valida.
    /// </summary>
    /// <param name="sourceJson">JSON con formato de array de programas. Ver documentación interna para el esquema.</param>
    Task<IReadOnlyList<EvaluationProgramDto>> FetchProgramsAsync(string sourceJson, CancellationToken ct = default);
}
