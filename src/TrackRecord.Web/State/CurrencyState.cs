using TrackRecord.Application.Abstractions;
using TrackRecord.Domain.Common;
using TrackRecord.Domain.Enums;

namespace TrackRecord.Web.State;

/// <summary>
/// Relé singleton entre instancias de <see cref="CurrencyState"/>. En Blazor Web App cada raíz de
/// interactividad (p.ej. el NavMenu embebido en un layout estático y el cuerpo de la página) recibe
/// su propio scope de DI, así que un servicio "scoped" como CurrencyState no se comparte entre ellas
/// aunque estén en el mismo circuito. Este relé reenvía los cambios de divisa a todas las instancias
/// vivas para que la página se repinte aunque el cambio se haya hecho desde otro scope (el NavMenu).
/// </summary>
public class CurrencyBroadcaster
{
    public event Action<Currency>? Changed;

    public void Notify(Currency currency) => Changed?.Invoke(currency);
}

/// <summary>
/// Estado de divisa de visualización. Se carga una vez por instancia y notifica a los suscriptores
/// locales (StateHasChanged) cuando la divisa cambia, ya sea desde esta misma instancia o desde otro
/// scope de DI (ver <see cref="CurrencyBroadcaster"/>).
/// </summary>
public class CurrencyState : IDisposable
{
    private readonly ICurrencyPreferenceService _preferenceService;
    private readonly CurrencyBroadcaster _broadcaster;
    private bool _loaded;

    public CurrencyState(ICurrencyPreferenceService preferenceService, CurrencyBroadcaster broadcaster)
    {
        _preferenceService = preferenceService;
        _broadcaster = broadcaster;
        _broadcaster.Changed += OnBroadcastChanged;
    }

    public Currency Current { get; private set; } = Currency.Usd;

    public event Action? Changed;

    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        Current = await _preferenceService.GetAsync();
        _loaded = true;
    }

    public async Task SetAsync(Currency currency)
    {
        if (Current == currency) return;
        Current = currency;
        _loaded = true;
        await _preferenceService.SetAsync(currency);
        Changed?.Invoke();
        _broadcaster.Notify(currency);
    }

    private void OnBroadcastChanged(Currency currency)
    {
        if (Current == currency) return;
        Current = currency;
        _loaded = true;
        Changed?.Invoke();
    }

    public string Format(decimal value, int decimals = 2) => CurrencyFormatter.Format(value, Current, decimals);
    public string Format(decimal? value, int decimals = 2) => CurrencyFormatter.Format(value, Current, decimals);
    public string Symbol => CurrencyFormatter.Symbol(Current);

    public void Dispose() => _broadcaster.Changed -= OnBroadcastChanged;
}
