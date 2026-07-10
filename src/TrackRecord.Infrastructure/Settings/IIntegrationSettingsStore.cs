namespace TrackRecord.Infrastructure.Settings;

/// <summary>
/// Almacén clave/valor cifrado en base de datos (tabla IntegrationSettings). Hoy guarda
/// preferencias por usuario como la divisa de visualización (ver CurrencyPreferenceService);
/// los valores se cifran con IDataProtector antes de persistirse — ver
/// DataProtectedIntegrationSettingsStore.
/// </summary>
public interface IIntegrationSettingsStore
{
    Task<string?> GetAsync(string key, CancellationToken ct = default);

    /// <summary>Un valor null o en blanco borra la clave.</summary>
    Task SetAsync(string key, string? value, CancellationToken ct = default);
}
