using Microsoft.EntityFrameworkCore;
using TrackRecord.Application.Abstractions;
using TrackRecord.Application.Kpis;
using TrackRecord.Domain.Enums;
using TrackRecord.Infrastructure.Persistence;

namespace TrackRecord.Infrastructure.Services;

/// <summary>
/// Ranking de perfiles Elite por ROI de negocio y datos agregados de su operativa para el módulo
/// de inspiración (F5.6). Consultas autocontenidas (no reutiliza los servicios acotados al usuario
/// actual, que no aceptan un userId de destino). Solo expone agregados no monetarios; el detalle
/// de operativa exige opt-in del dueño y plan Elite del que consulta.
/// </summary>
public class PeerDiscoveryService(
    IDbContextFactory<TrackRecordDbContext> dbFactory,
    ICurrentUserAccessor currentUser,
    IPlanService planService) : IPeerDiscoveryService
{
    public async Task<IReadOnlyList<PeerCardView>> GetLeaderboardAsync(int take = 20, CancellationToken ct = default)
    {
        var viewerId = await currentUser.RequireUserIdAsync();
        if (!(await planService.GetLimitsAsync(viewerId, ct)).CanBrowsePeers)
        {
            return [];
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var profiles = await db.PublicProfiles.AsNoTracking()
            .Where(p => p.IsEnabled && p.UserId != viewerId)
            .Select(p => new { p.UserId, p.Slug, p.ShareOperativa })
            .ToListAsync(ct);

        var cards = new List<PeerCardView>();
        foreach (var p in profiles)
        {
            // El downgrade retira el perfil del ranking sin borrarlo (se reactiva si vuelve a Elite).
            if (await planService.GetTierAsync(p.UserId, ct) != PlanTier.Elite) continue;

            var m = await BuildMetricsAsync(db, p.UserId, ct);
            if (m.BusinessRoi is null) continue; // sin coste de negocio no hay ROI que rankear

            cards.Add(new PeerCardView(
                p.Slug, m.DisplayName, m.BusinessRoi.Value, m.AccountsFunded, m.PassRate,
                m.ProfitFactor, m.WinRate, m.TotalTrades, m.IsVerified, p.ShareOperativa));
        }

        return cards.OrderByDescending(c => c.BusinessRoi).Take(take).ToList();
    }

    public async Task<PeerAnalysisView?> GetPeerAnalysisAsync(string slug, CancellationToken ct = default)
    {
        var viewerId = await currentUser.RequireUserIdAsync();
        if (!(await planService.GetLimitsAsync(viewerId, ct)).CanBrowsePeers)
        {
            return null;
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var profile = await db.PublicProfiles.AsNoTracking()
            .SingleOrDefaultAsync(p => p.Slug == slug && p.IsEnabled, ct);
        if (profile is null || profile.UserId == viewerId || !profile.ShareOperativa) return null;
        if (await planService.GetTierAsync(profile.UserId, ct) != PlanTier.Elite) return null;

        var m = await BuildMetricsAsync(db, profile.UserId, ct);
        var topSetups = await BuildTopSetupsAsync(db, profile.UserId, ct);
        var emotions = profile.ShareEmotions ? await BuildEmotionSummaryAsync(db, profile.UserId, ct) : null;

        return new PeerAnalysisView(
            profile.UserId, profile.Slug, m.DisplayName, m.IsVerified, m.BusinessRoi ?? 0, m.AccountsFunded,
            m.PassRate, m.TotalTrades, m.WinRate, m.ProfitFactor, m.AvgRMultiple, topSetups, m.EquityCurve, emotions);
    }

    private sealed record PeerMetrics(
        string DisplayName, int AccountsFunded, double? PassRate, int TotalTrades, double? WinRate,
        double? ProfitFactor, double? AvgRMultiple, bool IsVerified, double? BusinessRoi,
        IReadOnlyList<EquityCurvePoint> EquityCurve);

    private static async Task<PeerMetrics> BuildMetricsAsync(TrackRecordDbContext db, string userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.DisplayName, u.UserName })
            .SingleOrDefaultAsync(ct);
        var displayName = !string.IsNullOrWhiteSpace(user?.DisplayName) ? user!.DisplayName! : user?.UserName ?? "Trader";

        var accounts = await db.TradingAccounts.AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => new
            {
                a.Stage,
                a.FundedOn,
                Costs = a.Costs.Sum(c => (decimal?)c.Amount) ?? 0m,
                Payouts = a.Payouts.Sum(p => (decimal?)p.AmountReceived) ?? 0m,
            })
            .ToListAsync(ct);

        int funded = accounts.Count(a => a.Stage == AccountStage.Funded);
        int everFunded = accounts.Count(a => a.FundedOn is not null);
        int terminated = accounts.Count(a => a.FundedOn is not null || a.Stage is AccountStage.Failed or AccountStage.Expired);
        double? passRate = terminated > 0 ? (double)everFunded / terminated : null;

        decimal totalCosts = accounts.Sum(a => a.Costs);
        decimal totalPayouts = accounts.Sum(a => a.Payouts);
        double? roi = totalCosts > 0 ? (double)((totalPayouts - totalCosts) / totalCosts) : null;

        var trades = await db.Trades.AsNoTracking()
            .Where(t => t.Account!.UserId == userId)
            .Select(t => new { t.Id, t.ClosedAt, NetPnL = t.GrossPnL - t.Commissions, t.RiskedAmount })
            .ToListAsync(ct);

        int totalTrades = trades.Count;
        double? winRate = null, profitFactor = null, avgRMultiple = null;
        bool isVerified = false;
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

        return new PeerMetrics(displayName, funded, passRate, totalTrades, winRate, profitFactor, avgRMultiple, isVerified, roi, equityCurve);
    }

    private static async Task<IReadOnlyList<TagPerformanceDto>> BuildTopSetupsAsync(TrackRecordDbContext db, string userId, CancellationToken ct)
    {
        var trades = await db.Trades.AsNoTracking()
            .Where(t => t.Account!.UserId == userId && t.Tags != null && t.Tags != "")
            .Select(t => new { t.Tags, NetPnL = t.GrossPnL - t.Commissions })
            .ToListAsync(ct);

        return trades
            .SelectMany(t => t.Tags!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(tag => new { Tag = tag, t.NetPnL }))
            .GroupBy(x => x.Tag)
            .Select(g =>
            {
                var count = g.Count();
                var wins = g.Count(x => x.NetPnL > 0);
                decimal gp = g.Where(x => x.NetPnL > 0).Sum(x => x.NetPnL);
                decimal gl = Math.Abs(g.Where(x => x.NetPnL < 0).Sum(x => x.NetPnL));
                return new TagPerformanceDto(
                    g.Key, count, count > 0 ? (double)wins / count : null, g.Sum(x => x.NetPnL),
                    gl > 0 ? (double)(gp / gl) : null);
            })
            .OrderByDescending(t => t.NetPnL)
            .Take(5)
            .ToList();
    }

    private static async Task<PeerEmotionSummary?> BuildEmotionSummaryAsync(TrackRecordDbContext db, string userId, CancellationToken ct)
    {
        var logs = await db.TradeEmotionLogs.AsNoTracking()
            .Where(l => l.Trade!.Account!.UserId == userId)
            .Select(l => new { l.Moment, l.Emotion, l.Adherence })
            .ToListAsync(ct);
        if (logs.Count == 0) return null;

        var mostFrequent = logs
            .Where(l => l.Moment == EmotionMoment.BeforeEntry)
            .GroupBy(l => l.Emotion)
            .Select(g => new PeerEmotionFrequency(g.Key.ToString(), g.Count()))
            .OrderByDescending(e => e.Count)
            .Take(5)
            .ToList();

        double followedPct = (double)logs.Count(l => l.Adherence == PlanAdherence.FollowedPlan) / logs.Count * 100;
        return new PeerEmotionSummary(mostFrequent, Math.Round(followedPct, 0));
    }
}
