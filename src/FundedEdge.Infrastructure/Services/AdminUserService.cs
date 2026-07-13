using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FundedEdge.Application.Abstractions;
using FundedEdge.Domain.Enums;
using FundedEdge.Infrastructure.Identity;
using FundedEdge.Infrastructure.Persistence;

namespace FundedEdge.Infrastructure.Services;

/// <summary>Ver IAdminUserService. Solo consumido desde superficies protegidas con rol Administrator/Support.</summary>
public class AdminUserService(
    IDbContextFactory<FundedEdgeDbContext> dbFactory,
    UserManager<ApplicationUser> userManager,
    ICurrentUserAccessor currentUser) : IAdminUserService
{
    public async Task<IReadOnlyList<AdminUserSummary>> GetUsersAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var users = await db.Users
            .AsNoTracking()
            .OrderBy(u => u.Email)
            .Select(u => new
            {
                u.Id, u.Email, u.DisplayName, u.PlanTier, u.EmailConfirmed,
                Roles = db.UserRoles.Where(ur => ur.UserId == u.Id)
                    .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name!)
                    .ToList(),
                AccountCount = db.TradingAccounts.Count(a => a.UserId == u.Id),
            })
            .ToListAsync(ct);

        return users
            .Select(u => new AdminUserSummary(u.Id, u.Email, u.DisplayName, u.PlanTier, u.EmailConfirmed, u.Roles, u.AccountCount))
            .ToList();
    }

    public async Task<AdminUserSummary?> GetUserAsync(string userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var accountCount = await db.TradingAccounts.CountAsync(a => a.UserId == userId, ct);

        return new AdminUserSummary(user.Id, user.Email, user.DisplayName, user.PlanTier, user.EmailConfirmed, roles.ToList(), accountCount);
    }

    public async Task UpdateUserAsync(string userId, PlanTier plan, IReadOnlyCollection<string> roles, CancellationToken ct = default)
    {
        var unknown = roles.Except(AppRoles.All).ToList();
        if (unknown.Count > 0)
        {
            throw new InvalidOperationException($"Rol desconocido: {string.Join(", ", unknown)}.");
        }

        var user = await userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException("Usuario no encontrado.");

        // Anti-bloqueo: un administrador no puede quitarse a sí mismo el rol Administrator
        // (dejaría el panel sin acceso si es el único admin).
        var currentUserId = await currentUser.GetUserIdAsync();
        if (userId == currentUserId && !roles.Contains(AppRoles.Administrator))
        {
            throw new InvalidOperationException("No puedes quitarte a ti mismo el rol Administrator.");
        }

        if (user.PlanTier != plan)
        {
            user.PlanTier = plan;
            ThrowIfFailed(await userManager.UpdateAsync(user));
        }

        var existing = await userManager.GetRolesAsync(user);
        var toAdd = roles.Except(existing).ToList();
        var toRemove = existing.Except(roles).ToList();

        if (toAdd.Count > 0)
        {
            ThrowIfFailed(await userManager.AddToRolesAsync(user, toAdd));
        }

        if (toRemove.Count > 0)
        {
            ThrowIfFailed(await userManager.RemoveFromRolesAsync(user, toRemove));
        }

        if (toAdd.Count > 0 || toRemove.Count > 0)
        {
            // Rota el security stamp: IdentityRevalidatingAuthenticationStateProvider invalidará
            // la sesión activa del usuario, que al volver a entrar recibirá los claims de rol nuevos.
            ThrowIfFailed(await userManager.UpdateSecurityStampAsync(user));
        }
    }

    private static void ThrowIfFailed(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    public async Task<IReadOnlyList<AdminUserKpis>> GetUserKpisAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var rows = await db.Users
            .AsNoTracking()
            .Select(u => new
            {
                u.Id, u.Email, u.DisplayName, u.PlanTier,
                Accounts = db.TradingAccounts
                    .Where(a => a.UserId == u.Id)
                    .Select(a => new
                    {
                        a.Stage,
                        a.FundedOn,
                        Trades = a.Trades.Count,
                        Costs = a.Costs.Sum(c => (decimal?)c.Amount) ?? 0m,
                        Payouts = a.Payouts.Sum(p => (decimal?)p.AmountReceived) ?? 0m,
                    })
                    .ToList(),
            })
            .ToListAsync(ct);

        var kpis = rows.Select(u =>
        {
            var totalCosts = u.Accounts.Sum(a => a.Costs);
            var totalPayouts = u.Accounts.Sum(a => a.Payouts);
            // Activas = ni falladas, ni retiradas, ni expiradas (mismo criterio de etapa terminal
            // que TradingAccount.IsTerminal, sin cargar la entidad).
            var active = u.Accounts.Count(a => a.Stage is AccountStage.Evaluation or AccountStage.Funded);
            return new AdminUserKpis(
                u.Id, u.Email, u.DisplayName, u.PlanTier,
                AccountsPurchased: u.Accounts.Count,
                ActiveAccounts: active,
                FundedAccounts: u.Accounts.Count(a => a.FundedOn is not null),
                TotalTrades: u.Accounts.Sum(a => a.Trades),
                TotalCosts: totalCosts,
                TotalPayouts: totalPayouts,
                NetCashflow: totalPayouts - totalCosts,
                // Misma fórmula que KpiService.GetBusinessKpisAsync: (payouts - costes) / costes.
                BusinessRoi: totalCosts > 0 ? (double)((totalPayouts - totalCosts) / totalCosts) : null);
        });

        // Mejor ROI primero; los usuarios sin ROI calculable (sin costes) al final.
        return kpis
            .OrderByDescending(k => k.BusinessRoi.HasValue)
            .ThenByDescending(k => k.BusinessRoi)
            .ToList();
    }

    public async Task DeleteUserAndDataAsync(string userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException("Usuario no encontrado.");

        if (await userManager.IsInRoleAsync(user, AppRoles.Administrator))
        {
            throw new InvalidOperationException("No se puede eliminar a un administrador desde el panel.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // Orden explícito hijo→padre: las FKs de cuentas/trades son Restrict, así que no se puede
        // confiar en cascadas de BD. TradeEmotionLogs sí cascada al borrar el Trade.
        await db.Executions.Where(e => e.Account!.UserId == userId).ExecuteDeleteAsync(ct);
        await db.TradeEmotionLogs.Where(l => l.Trade!.Account!.UserId == userId).ExecuteDeleteAsync(ct);
        await db.Trades.Where(t => t.Account!.UserId == userId).ExecuteDeleteAsync(ct);
        await db.AccountEvents.Where(e => e.Account!.UserId == userId).ExecuteDeleteAsync(ct);
        await db.AccountCosts.Where(c => c.Account!.UserId == userId).ExecuteDeleteAsync(ct);
        await db.Payouts.Where(p => p.Account!.UserId == userId).ExecuteDeleteAsync(ct);
        await db.TradingAccounts.Where(a => a.UserId == userId).ExecuteDeleteAsync(ct);
        await db.AiReports.Where(r => r.UserId == userId).ExecuteDeleteAsync(ct);
        await db.PublicProfiles.Where(p => p.UserId == userId).ExecuteDeleteAsync(ct);
        await db.DailyMindsetCheckIns.Where(c => c.UserId == userId).ExecuteDeleteAsync(ct);
        // Los ajustes cifrados se clavean como "{userId}:..." (ver CurrencyPreferenceService).
        await db.IntegrationSettings.Where(s => s.Key.StartsWith(userId + ":")).ExecuteDeleteAsync(ct);

        // El propio usuario (Identity cascada logins, claims, tokens y roles en BD).
        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}
