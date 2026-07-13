namespace FundedEdge.Application.Abstractions;

/// <summary>
/// Contexto ambiental (AsyncLocal) para que trabajo en segundo plano sin circuito de Blazor activo
/// (p.ej. el informe semanal de IA, que recorre todos los usuarios uno a uno) pueda "actuar como"
/// un usuario concreto. Las implementaciones de ICurrentUserAccessor deben consultar
/// <see cref="OverrideUserId"/> antes de resolver el usuario por su vía habitual (cookie de sesión).
/// </summary>
public static class CurrentUserContext
{
    private static readonly AsyncLocal<string?> Override = new();

    public static string? OverrideUserId => Override.Value;

    public static IDisposable Impersonate(string userId)
    {
        var previous = Override.Value;
        Override.Value = userId;
        return new Restorer(previous);
    }

    private sealed class Restorer(string? previous) : IDisposable
    {
        public void Dispose() => Override.Value = previous;
    }
}
