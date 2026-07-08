using TrackRecord.Infrastructure.Settings;

namespace TrackRecord.Infrastructure.Integrations.Tradovate;

/// <summary>
/// Lee las credenciales de Tradovate de un usuario guardadas cifradas en base de datos desde
/// /settings. Las claves se prefijan con el Id del usuario ("{userId}:Tradovate:Name", etc.) para
/// que cada uno tenga las suyas sin necesitar una tabla dedicada — es la fuente preferente de
/// TradovateCredentialStore; si no hay nada guardado, se recurre a ConfigurationTradovateCredentialStore.
/// </summary>
public class DbTradovateCredentialStore(IIntegrationSettingsStore settingsStore) : ITradovateCredentialStore
{
    public async Task<TradovateCredentials?> GetCredentialsAsync(string userId, CancellationToken ct = default)
    {
        var name = await settingsStore.GetAsync(Key(userId, "Name"), ct);
        var password = await settingsStore.GetAsync(Key(userId, "Password"), ct);
        var cidRaw = await settingsStore.GetAsync(Key(userId, "Cid"), ct);
        var sec = await settingsStore.GetAsync(Key(userId, "Sec"), ct);

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(cidRaw) || string.IsNullOrWhiteSpace(sec) ||
            !int.TryParse(cidRaw, out var cid))
        {
            return null;
        }

        var deviceId = await settingsStore.GetAsync(Key(userId, "DeviceId"), ct) ?? TradovateDeviceId.Stable();

        return new TradovateCredentials(name, password, cid, sec, deviceId);
    }

    public static string Key(string userId, string suffix) => $"{userId}:Tradovate:{suffix}";
}
