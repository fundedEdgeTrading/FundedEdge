using Microsoft.Extensions.Configuration;

namespace TrackRecord.Infrastructure.Integrations.Tradovate;

/// <summary>
/// Lee las credenciales de Tradovate desde configuración: "Tradovate:Name",
/// "Tradovate:Password", "Tradovate:Cid", "Tradovate:Sec". Nunca desde appsettings.json
/// versionado — usar User Secrets o variables de entorno (ver README). Es el fallback de
/// TradovateCredentialStore cuando el usuario no ha guardado nada desde /settings — compartido
/// por todos los usuarios de la instancia, pensado para un despliegue autoalojado de un operador.
/// </summary>
public class ConfigurationTradovateCredentialStore(IConfiguration configuration) : ITradovateCredentialStore
{
    public Task<TradovateCredentials?> GetCredentialsAsync(string userId, CancellationToken ct = default)
    {
        var name = configuration["Tradovate:Name"];
        var password = configuration["Tradovate:Password"];
        var cidRaw = configuration["Tradovate:Cid"];
        var sec = configuration["Tradovate:Sec"];

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(cidRaw) || string.IsNullOrWhiteSpace(sec) ||
            !int.TryParse(cidRaw, out var cid))
        {
            return Task.FromResult<TradovateCredentials?>(null);
        }

        var deviceId = configuration["Tradovate:DeviceId"] ?? TradovateDeviceId.Stable();

        return Task.FromResult<TradovateCredentials?>(new TradovateCredentials(name, password, cid, sec, deviceId));
    }
}
