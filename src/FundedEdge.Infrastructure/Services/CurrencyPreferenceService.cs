using FundedEdge.Application.Abstractions;
using FundedEdge.Domain.Enums;
using FundedEdge.Infrastructure.Settings;

namespace FundedEdge.Infrastructure.Services;

/// <summary>
/// Reutiliza el almacén clave/valor de IntegrationSettings (ya persistido en base de datos) para
/// guardar la divisa de visualización elegida por cada usuario. No es un secreto, pero comparte
/// infraestructura en vez de crear una tabla nueva para una única preferencia de UI.
/// </summary>
public class CurrencyPreferenceService(IIntegrationSettingsStore settingsStore, ICurrentUserAccessor currentUser) : ICurrencyPreferenceService
{
    public async Task<Currency> GetAsync(CancellationToken ct = default)
    {
        var userId = await currentUser.GetUserIdAsync();
        if (userId is null) return Currency.Usd;

        var raw = await settingsStore.GetAsync(Key(userId), ct);
        return Enum.TryParse<Currency>(raw, ignoreCase: true, out var currency) ? currency : Currency.Usd;
    }

    public async Task SetAsync(Currency currency, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await settingsStore.SetAsync(Key(userId), currency.ToString(), ct);
    }

    private static string Key(string userId) => $"{userId}:Ui:Currency";
}
