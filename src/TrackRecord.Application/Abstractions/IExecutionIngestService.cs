using TrackRecord.Application.Dtos;

namespace TrackRecord.Application.Abstractions;

/// <summary>
/// Punto de entrada único para ingestar fills crudos de fuentes externas (NinjaTrader 8 vía
/// push HTTP, Tradovate vía polling). Idempotente por (Source, ExternalId).
/// </summary>
public interface IExecutionIngestService
{
    /// <summary>userId acota la resolución de la cuenta por AccountExternalId a las del propio usuario.</summary>
    Task<IngestExecutionResult> IngestAsync(IngestExecutionRequest request, string userId, CancellationToken ct = default);
}
