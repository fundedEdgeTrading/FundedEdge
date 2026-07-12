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
/// Es singleton (compartido por todos los usuarios conectados al servidor), así que cada notificación
/// lleva el id del usuario que la originó para que solo sus propios scopes reaccionen.
/// </summary>
public class CurrencyBroadcaster
{
    public event Action<string, Currency>? Changed;

    public void Notify(string userId, Currency currency) => Changed?.Invoke(userId, currency);
}

/// <summary>
/// Estado de divisa de visualización. Se carga una vez por instancia y notifica a los suscriptores
/// locales (StateHasChanged) cuando la divisa cambia, ya sea desde esta misma instancia o desde otro
/// scope de DI (ver <see cref="CurrencyBroadcaster"/>).
/// </summary>
public class CurrencyState : IDisposable
{
    private readonly ICurrencyPreferenceService _preferenceService;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly CurrencyBroadcaster _broadcaster;
    private bool _loaded;
    private string? _userId;

    public CurrencyState(ICurrencyPreferenceService preferenceService, ICurrentUserAccessor currentUser, CurrencyBroadcaster broadcaster)
    {
        _preferenceService = preferenceService;
        _currentUser = currentUser;
        _broadcaster = broadcaster;
        _broadcaster.Changed += OnBroadcastChanged;
    }

    public Currency Current { get; private set; } = Currency.Usd;

    public event Action? Changed;

    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        _userId = await _currentUser.GetUserIdAsync();
        Current = await _preferenceService.GetAsync();
        _loaded = true;
    }

    public async Task SetAsync(Currency currency)
    {
        if (Current == currency) return;
        Current = currency;
        _loaded = true;
        _userId ??= await _currentUser.GetUserIdAsync();
        await _preferenceService.SetAsync(currency);
        Changed?.Invoke();
        if (_userId is not null)
        {
            _broadcaster.Notify(_userId, currency);
        }
    }

    private void OnBroadcastChanged(string userId, Currency currency) => _ = HandleBroadcastAsync(userId, currency);

    private async Task HandleBroadcastAsync(string userId, Currency currency)
    {
        // Instancias de página nunca llaman a EnsureLoadedAsync (solo el NavMenu lo hace), así que
        // resolvemos el usuario aquí, la primera vez que hace falta comparar contra un broadcast.
        _userId ??= await _currentUser.GetUserIdAsync();
        if (userId != _userId || Current == currency) return;
        Current = currency;
        _loaded = true;
        Changed?.Invoke();
    }

    public string Format(decimal value, int decimals = 2) => CurrencyFormatter.Format(value, Current, decimals);
    public string Format(decimal? value, int decimals = 2) => CurrencyFormatter.Format(value, Current, decimals);
    public string Symbol => CurrencyFormatter.Symbol(Current);

    public void Dispose() => _broadcaster.Changed -= OnBroadcastChanged;
}
