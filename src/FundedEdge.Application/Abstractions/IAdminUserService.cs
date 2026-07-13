using FundedEdge.Domain.Enums;

namespace FundedEdge.Application.Abstractions;

/// <summary>Fila del listado de usuarios del panel de administración.</summary>
public record AdminUserSummary(
    string Id,
    string? Email,
    string? DisplayName,
    PlanTier PlanTier,
    bool EmailConfirmed,
    IReadOnlyList<string> Roles,
    int AccountCount);

/// <summary>KPIs de negocio de un usuario para la vista de tarjetas del panel de administración.</summary>
public record AdminUserKpis(
    string Id,
    string? Email,
    string? DisplayName,
    PlanTier PlanTier,
    int AccountsPurchased,
    int ActiveAccounts,
    int FundedAccounts,
    int TotalTrades,
    decimal TotalCosts,
    decimal TotalPayouts,
    decimal NetCashflow,
    double? BusinessRoi);

/// <summary>
/// Operaciones de administración sobre usuarios. SOLO debe invocarse desde superficies
/// protegidas con el rol Administrator (páginas /admin y endpoints /admin/*).
/// </summary>
public interface IAdminUserService
{
    Task<IReadOnlyList<AdminUserSummary>> GetUsersAsync(CancellationToken ct = default);

    Task<AdminUserSummary?> GetUserAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Cambia el plan y los roles de un usuario. Los roles deben existir (ver AppRoles); un
    /// administrador no puede quitarse a sí mismo el rol Administrator. Si los roles cambian se
    /// rota el security stamp para que la sesión del usuario se refresque con los nuevos permisos.
    /// </summary>
    Task UpdateUserAsync(string userId, PlanTier plan, IReadOnlyCollection<string> roles, CancellationToken ct = default);

    /// <summary>KPIs por usuario, ordenados de mayor a menor ROI de negocio (los sin ROI al final).</summary>
    Task<IReadOnlyList<AdminUserKpis>> GetUserKpisAsync(CancellationToken ct = default);

    /// <summary>
    /// Elimina un usuario y TODOS sus datos (cuentas, trades, ejecuciones, costes, payouts,
    /// informes de IA, perfil público, psicología y ajustes). Irreversible.
    /// </summary>
    Task DeleteUserAndDataAsync(string userId, CancellationToken ct = default);
}
