namespace TrackRecord.Infrastructure.Integrations.Tradovate;

/// <summary>
/// Implementación compuesta de ITradovateCredentialStore registrada en DI: prioriza lo guardado
/// desde /settings por el propio usuario (base de datos, cifrado) y cae a configuración
/// (user-secrets/entorno, compartida) si no hay nada guardado allí.
/// </summary>
public class TradovateCredentialStore(
    DbTradovateCredentialStore dbStore,
    ConfigurationTradovateCredentialStore configStore) : ITradovateCredentialStore
{
    public async Task<TradovateCredentials?> GetCredentialsAsync(string userId, CancellationToken ct = default) =>
        await dbStore.GetCredentialsAsync(userId, ct) ?? await configStore.GetCredentialsAsync(userId, ct);
}
