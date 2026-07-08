using TrackRecord.Application.Dtos;

namespace TrackRecord.Application.Abstractions;

/// <summary>
/// Import CSV universal con mapeo de columnas asistido (GUIA_FUNCIONALIDADES_PROPUESTAS.md §4.6):
/// a diferencia de <see cref="ICsvTradeImportService"/> (atado al export de NinjaTrader 8), este
/// servicio acepta cualquier CSV de round-turns siempre que el usuario indique qué columna
/// corresponde a cada campo. Cada broker nuevo soportado sin código adicional.
/// </summary>
public interface IGenericCsvImportService
{
    /// <summary>Lee solo la fila de cabecera para poblar el formulario de mapeo de columnas.</summary>
    Task<IReadOnlyList<string>> ReadHeadersAsync(Stream csvStream, CancellationToken ct = default);

    Task<CsvImportSummary> ImportAsync(Guid accountId, Stream csvStream, GenericCsvColumnMapping mapping, CancellationToken ct = default);
}
