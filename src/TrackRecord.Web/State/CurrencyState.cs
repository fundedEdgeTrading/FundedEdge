using TrackRecord.Application.Abstractions;
using TrackRecord.Domain.Common;
using TrackRecord.Domain.Enums;

namespace TrackRecord.Web.State;

/// <summary>
/// Estado de divisa de visualización compartido dentro del circuito (scoped). Se carga una vez por
/// sesión y notifica a los suscriptores (p.ej. NavMenu) cuando el usuario la cambia, para que
/// puedan refrescar la página actual sin perder la posición en la navegación.
/// </summary>
public class CurrencyState(ICurrencyPreferenceService preferenceService)
{
    private bool _loaded;

    public Currency Current { get; private set; } = Currency.Usd;

    public event Action? Changed;

    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        Current = await preferenceService.GetAsync();
        _loaded = true;
    }

    public async Task SetAsync(Currency currency)
    {
        if (Current == currency) return;
        Current = currency;
        _loaded = true;
        await preferenceService.SetAsync(currency);
        Changed?.Invoke();
    }

    public string Format(decimal value, int decimals = 2) => CurrencyFormatter.Format(value, Current, decimals);
    public string Format(decimal? value, int decimals = 2) => CurrencyFormatter.Format(value, Current, decimals);
    public string Symbol => CurrencyFormatter.Symbol(Current);
}
