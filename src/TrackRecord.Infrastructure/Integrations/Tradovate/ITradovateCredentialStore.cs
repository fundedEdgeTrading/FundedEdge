namespace TrackRecord.Infrastructure.Integrations.Tradovate;

/// <summary>
/// Fuente de las credenciales de Tradovate de un usuario concreto. La implementación por defecto
/// (DbTradovateCredentialStore) las lee cifradas de lo guardado desde /settings; si el usuario no
/// ha configurado las suyas, se recurre a ConfigurationTradovateCredentialStore (user-secrets/entorno,
/// un fallback compartido pensado para instancias autoalojadas de un solo operador).
/// </summary>
public interface ITradovateCredentialStore
{
    Task<TradovateCredentials?> GetCredentialsAsync(string userId, CancellationToken ct = default);
}
