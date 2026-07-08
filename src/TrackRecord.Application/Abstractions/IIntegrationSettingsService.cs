using TrackRecord.Application.Dtos;

namespace TrackRecord.Application.Abstractions;

/// <summary>
/// Fachada usada por la página /settings para leer y guardar credenciales de integraciones.
/// Las credenciales de Tradovate y la API key de ingesta de NinjaTrader se guardan cifradas en
/// base de datos (IDataProtector); la clave de Anthropic nunca se guarda aquí — solo se reporta
/// su estado, y se configura vía User Secrets/entorno (ver README).
/// </summary>
public interface IIntegrationSettingsService
{
    Task<TradovateSettingsDto> GetTradovateSettingsAsync(CancellationToken ct = default);
    Task SaveTradovateSettingsAsync(SaveTradovateSettingsRequest request, CancellationToken ct = default);
    Task ClearTradovateSettingsAsync(CancellationToken ct = default);

    Task<IngestSettingsDto> GetIngestSettingsAsync(CancellationToken ct = default);
    Task SaveIngestApiKeyAsync(string apiKey, CancellationToken ct = default);
    Task ClearIngestApiKeyAsync(CancellationToken ct = default);

    /// <summary>Genera una API key aleatoria criptográficamente segura, la guarda y la devuelve una sola vez.</summary>
    Task<string> GenerateIngestApiKeyAsync(CancellationToken ct = default);

    Task<AiSettingsStatusDto> GetAiSettingsStatusAsync(CancellationToken ct = default);
}
