namespace TrackRecord.Infrastructure.Settings;

/// <summary>
/// Almacén clave/valor cifrado en base de datos para credenciales de integraciones configuradas
/// desde /settings (Tradovate, API key de ingesta NinjaTrader). Los valores se cifran con
/// IDataProtector antes de persistirse — ver DataProtectedIntegrationSettingsStore.
/// </summary>
public interface IIntegrationSettingsStore
{
    Task<string?> GetAsync(string key, CancellationToken ct = default);

    /// <summary>Un valor null o en blanco borra la clave.</summary>
    Task SetAsync(string key, string? value, CancellationToken ct = default);

    /// <summary>
    /// Busca, entre las claves que terminan en <paramref name="keySuffix"/>, la primera cuyo valor
    /// descifrado coincide con <paramref name="value"/>, y devuelve el prefijo de esa clave (el
    /// UserId, dado el convenio "{userId}:...") o null si ninguna coincide. Usada para resolver de
    /// qué usuario es una API key entrante (p.ej. ingesta de NinjaTrader) sin saber de antemano
    /// quién la generó.
    /// </summary>
    Task<string?> FindKeyPrefixByValueAsync(string keySuffix, string value, CancellationToken ct = default);
}
