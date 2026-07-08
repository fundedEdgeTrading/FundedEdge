using System.Globalization;
using TrackRecord.Application.Abstractions;
using TrackRecord.Application.Dtos;
using TrackRecord.Infrastructure.Ai;
using TrackRecord.Infrastructure.Integrations.Tradovate;
using TrackRecord.Infrastructure.Settings;

namespace TrackRecord.Infrastructure.Services;

/// <summary>
/// Las credenciales guardadas desde /settings se cifran con la misma clave de valor prefijada por
/// usuario que usa DbTradovateCredentialStore ("{userId}:Tradovate:*", "{userId}:Ingest:*"), así
/// cada usuario tiene las suyas sin necesitar una tabla dedicada.
/// </summary>
public class IntegrationSettingsService(
    IIntegrationSettingsStore settingsStore,
    ConfigurationTradovateCredentialStore configTradovateStore,
    ICurrentUserAccessor currentUser,
    AiOptions aiOptions) : IIntegrationSettingsService
{
    private static readonly string[] TradovateKeySuffixes = ["Name", "Password", "Cid", "Sec", "DeviceId"];

    public async Task<TradovateSettingsDto> GetTradovateSettingsAsync(CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();

        var dbName = await settingsStore.GetAsync(DbTradovateCredentialStore.Key(userId, "Name"), ct);
        if (!string.IsNullOrWhiteSpace(dbName))
        {
            var cidRaw = await settingsStore.GetAsync(DbTradovateCredentialStore.Key(userId, "Cid"), ct);
            return new TradovateSettingsDto(true, dbName, int.TryParse(cidRaw, out var cid) ? cid : null, "Base de datos (/settings)");
        }

        var configCreds = await configTradovateStore.GetCredentialsAsync(userId, ct);
        if (configCreds is not null)
        {
            return new TradovateSettingsDto(true, configCreds.Name, configCreds.Cid, "Configuración (User Secrets/entorno)");
        }

        return new TradovateSettingsDto(false, null, null, "Sin configurar");
    }

    public async Task SaveTradovateSettingsAsync(SaveTradovateSettingsRequest request, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await settingsStore.SetAsync(DbTradovateCredentialStore.Key(userId, "Name"), request.Name, ct);
        await settingsStore.SetAsync(DbTradovateCredentialStore.Key(userId, "Password"), request.Password, ct);
        await settingsStore.SetAsync(DbTradovateCredentialStore.Key(userId, "Cid"), request.Cid.ToString(CultureInfo.InvariantCulture), ct);
        await settingsStore.SetAsync(DbTradovateCredentialStore.Key(userId, "Sec"), request.Sec, ct);
        await settingsStore.SetAsync(DbTradovateCredentialStore.Key(userId, "DeviceId"), request.DeviceId, ct);
    }

    public async Task ClearTradovateSettingsAsync(CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        foreach (var suffix in TradovateKeySuffixes)
        {
            await settingsStore.SetAsync(DbTradovateCredentialStore.Key(userId, suffix), null, ct);
        }
    }

    public async Task<IngestSettingsDto> GetIngestSettingsAsync(CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();

        var dbKey = await settingsStore.GetAsync(IngestApiKeyKey(userId), ct);
        if (!string.IsNullOrWhiteSpace(dbKey))
        {
            return new IngestSettingsDto(true, "Base de datos (/settings)");
        }

        return new IngestSettingsDto(false, "Sin configurar");
    }

    /// <summary>Entropía mínima exigida a una API key de ingesta introducida a mano. [SEC-08]</summary>
    private const int MinIngestApiKeyLength = 24;

    public async Task SaveIngestApiKeyAsync(string apiKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Trim().Length < MinIngestApiKeyLength)
        {
            throw new ArgumentException(
                $"La API key debe tener al menos {MinIngestApiKeyLength} caracteres. Usa el botón de generación para crear una segura.",
                nameof(apiKey));
        }

        var userId = await currentUser.RequireUserIdAsync();
        await settingsStore.SetAsync(IngestApiKeyKey(userId), apiKey.Trim(), ct);
    }

    public async Task<string> GenerateIngestApiKeyAsync(CancellationToken ct = default)
    {
        // 32 bytes → 256 bits de entropía, en hexadecimal (seguro en cabeceras HTTP, sin
        // dependencias del framework web). Se guarda cifrada y se devuelve al llamante para
        // mostrarla una sola vez.
        var apiKey = Convert.ToHexString(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

        var userId = await currentUser.RequireUserIdAsync();
        await settingsStore.SetAsync(IngestApiKeyKey(userId), apiKey, ct);
        return apiKey;
    }

    public async Task ClearIngestApiKeyAsync(CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await settingsStore.SetAsync(IngestApiKeyKey(userId), null, ct);
    }

    public Task<AiSettingsStatusDto> GetAiSettingsStatusAsync(CancellationToken ct = default) =>
        Task.FromResult(new AiSettingsStatusDto(aiOptions.IsApiKeyConfigured));

    private static string IngestApiKeyKey(string userId) => $"{userId}:Ingest:NinjaTraderApiKey";
}
