using FundedEdge.Application.Dtos;

namespace FundedEdge.Application.Abstractions;

/// <summary>
/// Importa el CSV de "Trade Performance" exportado desde NinjaTrader 8 (round-turns ya
/// agregados, no fills sueltos) — ver GUIA_IMPLEMENTACION.md §6, Opción B. Idempotente: cada
/// fila se identifica por un hash determinista de su contenido, así que reimportar el mismo
/// archivo no duplica trades.
/// </summary>
public interface ICsvTradeImportService
{
    Task<CsvImportSummary> ImportAsync(Guid accountId, Stream csvStream, CancellationToken ct = default);
}
