namespace TrackRecord.Application.Abstractions;

/// <summary>
/// Resuelve el usuario autenticado en el contexto actual. Todos los servicios que devuelven o
/// modifican datos propios de un usuario (cuentas, trades, informes de IA, credenciales de
/// integraciones) lo usan para aislar los datos entre usuarios. Implementado en la capa Web con
/// AuthenticationStateProvider (funciona en todo el circuito de Blazor Server, no solo en la
/// petición HTTP inicial).
/// </summary>
public interface ICurrentUserAccessor
{
    /// <summary>Id del usuario autenticado, o null si no hay nadie autenticado en este contexto.</summary>
    Task<string?> GetUserIdAsync();

    /// <summary>Como GetUserIdAsync, pero lanza si no hay usuario autenticado — para operaciones que lo requieren.</summary>
    async Task<string> RequireUserIdAsync()
    {
        var userId = await GetUserIdAsync();
        return userId ?? throw new InvalidOperationException("Esta operación requiere un usuario autenticado.");
    }
}
