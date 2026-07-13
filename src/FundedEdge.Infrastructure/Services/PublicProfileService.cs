using Microsoft.EntityFrameworkCore;
using FundedEdge.Application.Abstractions;
using FundedEdge.Application.Kpis;
using FundedEdge.Domain.Entities;
using FundedEdge.Domain.Enums;
using FundedEdge.Infrastructure.Persistence;

namespace FundedEdge.Infrastructure.Services;

public class PublicProfileService(
    IDbContextFactory<FundedEdgeDbContext> dbFactory,
    ICurrentUserAccessor currentUser,
    IPlanService planService) : IPublicProfileService
{
    public async Task<PublicProfileSettings> GetOwnSettingsAsync(CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        var limits = await planService.GetLimitsAsync(userId, ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var profile = await db.PublicProfiles.AsNoTracking().SingleOrDefaultAsync(p => p.UserId == userId, ct);
        return new PublicProfileSettings(profile?.Slug, profile?.IsEnabled ?? false, limits.CanPublishPublicProfile);
    }

    public async Task<PublicProfileSettings> EnableAsync(CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        var limits = await planService.GetLimitsAsync(userId, ct);
        if (!limits.CanPublishPublicProfile)
        {
            throw new InvalidOperationException("La página pública de track record requiere el plan Elite.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var profile = await db.PublicProfiles.SingleOrDefaultAsync(p => p.UserId == userId, ct);

        if (profile is null)
        {
            profile = new PublicProfile
            {
                UserId = userId,
                Slug = await GenerateUniqueSlugAsync(db, ct),
                CreatedAt = DateTimeOffset.UtcNow,
            };
            db.PublicProfiles.Add(profile);
        }

        profile.IsEnabled = true;
        await db.SaveChangesAsync(ct);
        return new PublicProfileSettings(profile.Slug, true, true);
    }

    public async Task DisableAsync(CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var profile = await db.PublicProfiles.SingleOrDefaultAsync(p => p.UserId == userId, ct);
        if (profile is null) return;

        profile.IsEnabled = false;
        await db.SaveChangesAsync(ct);
    }

    public async Task<PublicProfileView?> GetPublicViewAsync(string slug, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var profile = await db.PublicProfiles.AsNoTracking()
            .SingleOrDefaultAsync(p => p.Slug == slug && p.IsEnabled, ct);
        if (profile is null) return null;

        // El downgrade a un plan inferior retira la página sin necesidad de borrarla (se reactiva si vuelve a Elite).
        var limits = await planService.GetLimitsAsync(profile.UserId, ct);
        if (!limits.CanPublishPublicProfile) return null;

        var user = await db.Users.AsNoTracking()
            .Select(u => new { u.Id, u.DisplayName, u.UserName })
            .SingleOrDefaultAsync(u => u.Id == profile.UserId, ct);
        if (user is null) return null;

        var accounts = await db.TradingAccounts.AsNoTracking()
            .Where(a => a.UserId == profile.UserId)
            .Select(a => new { a.Stage, a.FundedOn })
            .ToListAsync(ct);

        int funded = accounts.Count(a => a.Stage == AccountStage.Funded);
        int everFunded = accounts.Count(a => a.FundedOn is not null);
        int terminated = accounts.Count(a => a.FundedOn is not null || a.Stage is AccountStage.Failed or AccountStage.Expired);
        double? passRate = terminated > 0 ? (double)everFunded / terminated : null;

        var trades = await db.Trades.AsNoTracking()
            .Where(t => t.Account!.UserId == profile.UserId)
            .Select(t => new { t.Id, t.ClosedAt, NetPnL = t.GrossPnL - t.Commissions, t.RiskedAmount })
            .ToListAsync(ct);

        int totalTrades = trades.Count;
        double? winRate = null, profitFactor = null, avgRMultiple = null;
        if (totalTrades > 0)
        {
            var wins = trades.Where(t => t.NetPnL > 0).ToList();
            var losses = trades.Where(t => t.NetPnL < 0).ToList();
            winRate = (double)wins.Count / totalTrades;

            decimal grossProfit = wins.Sum(t => t.NetPnL);
            decimal grossLoss = Math.Abs(losses.Sum(t => t.NetPnL));
            profitFactor = grossLoss > 0 ? (double)(grossProfit / grossLoss) : null;

            var rMultiples = trades.Where(t => t.RiskedAmount is > 0)
                .Select(t => (double)(t.NetPnL / t.RiskedAmount!.Value))
                .ToList();
            avgRMultiple = rMultiples.Count > 0 ? rMultiples.Average() : null;
        }

        var displayName = !string.IsNullOrWhiteSpace(user.DisplayName) ? user.DisplayName : user.UserName ?? "Trader";

        // "Verificado" = la mayoría de los trades vienen del export oficial de la plataforma
        // (CSV de Tradovate/NinjaTrader 8), no introducidos a mano — GUIA_FUNCIONALIDADES_PROPUESTAS.md §3.7.
        bool isVerified = false;
        if (totalTrades > 0)
        {
            var tradeIds = trades.Select(t => t.Id).ToList();
            var verifiedTradeCount = await db.Executions.AsNoTracking()
                .Where(e => e.TradeId != null && tradeIds.Contains(e.TradeId!.Value) && e.Source != TradeSourceType.Manual)
                .Select(e => e.TradeId!.Value)
                .Distinct()
                .CountAsync(ct);
            isVerified = (double)verifiedTradeCount / totalTrades >= 0.8;
        }

        var equityCurve = new List<EquityCurvePoint>();
        decimal cumulative = 0m;
        foreach (var day in trades.OrderBy(t => t.ClosedAt).GroupBy(t => DateOnly.FromDateTime(t.ClosedAt.Date)))
        {
            cumulative += day.Sum(t => t.NetPnL);
            equityCurve.Add(new EquityCurvePoint(day.Key, cumulative));
        }

        return new PublicProfileView(displayName, funded, passRate, totalTrades, winRate, profitFactor, avgRMultiple, isVerified, equityCurve);
    }

    private static async Task<string> GenerateUniqueSlugAsync(FundedEdgeDbContext db, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var candidate = Guid.NewGuid().ToString("N")[..8];
            if (!await db.PublicProfiles.AnyAsync(p => p.Slug == candidate, ct))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("No se pudo generar un slug único para la página pública.");
    }
}
