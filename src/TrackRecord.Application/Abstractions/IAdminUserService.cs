using TrackRecord.Domain.Enums;

namespace TrackRecord.Application.Abstractions;

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

    /// <summary>KPIs por usuario, ordenados de mayor a menor ROI de negocio (los sin ROI al final).</summary>
    Task<IReadOnlyList<AdminUserKpis>> GetUserKpisAsync(CancellationToken ct = default);

    /// <summary>
    /// Elimina un usuario y TODOS sus datos (cuentas, trades, ejecuciones, costes, payouts,
    /// informes de IA, perfil público, psicología y ajustes). Irreversible.
    /// </summary>
    Task DeleteUserAndDataAsync(string userId, CancellationToken ct = default);
}
