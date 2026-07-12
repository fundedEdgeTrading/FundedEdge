using Microsoft.EntityFrameworkCore;
using TrackRecord.Application.Abstractions;
using TrackRecord.Application.Kpis;
using TrackRecord.Domain.Enums;
using TrackRecord.Infrastructure.Persistence;

namespace TrackRecord.Infrastructure.Services;

public class KpiService(IDbContextFactory<TrackRecordDbContext> dbFactory, ICurrentUserAccessor currentUser) : IKpiService
{
    public async Task<BusinessKpis> GetBusinessKpisAsync(Guid? propFirmId = null, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var accounts = await db.TradingAccounts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Where(a => propFirmId == null || a.PropFirmId == propFirmId)
            .Select(a => new
            {
                a.Stage,
                a.FundedOn,
                TotalCosts = a.Costs.Sum(c => (decimal?)c.Amount) ?? 0m,
                TotalPayouts = a.Payouts.Sum(p => (decimal?)p.AmountReceived) ?? 0m,
            })
            .ToListAsync(ct);

        int purchased = accounts.Count;
        int inEvaluation = accounts.Count(a => a.Stage == AccountStage.Evaluation);
        int funded = accounts.Count(a => a.Stage == AccountStage.Funded);
        int failed = accounts.Count(a => a.Stage == AccountStage.Failed);
        int withdrawn = accounts.Count(a => a.Stage == AccountStage.Withdrawn);
        int expired = accounts.Count(a => a.Stage == AccountStage.Expired);

        int everFunded = accounts.Count(a => a.FundedOn is not null);
        int terminated = accounts.Count(a => a.FundedOn is not null || a.Stage is AccountStage.Failed or AccountStage.Expired);

        decimal totalCosts = accounts.Sum(a => a.TotalCosts);
        decimal totalPayouts = accounts.Sum(a => a.TotalPayouts);

        return new BusinessKpis(
            AccountsPurchased: purchased,
            AccountsInEvaluation: inEvaluation,
            AccountsFunded: funded,
            AccountsFailed: failed,
            AccountsWithdrawn: withdrawn,
            AccountsExpired: expired,
            EvaluationsTerminated: terminated,
            PassRate: terminated > 0 ? (double)everFunded / terminated : null,
            TotalCosts: totalCosts,
            TotalPayoutsReceived: totalPayouts,
            NetCashflow: totalPayouts - totalCosts,
            CostPerFundedAccount: everFunded > 0 ? totalCosts / everFunded : null,
            AvgPayoutPerFundedAccount: everFunded > 0 ? totalPayouts / everFunded : null,
            BusinessRoi: totalCosts > 0 ? (double)((totalPayouts - totalCosts) / totalCosts) : null);
    }

    public async Task<TradingKpis> GetTradingKpisAsync(Guid? propFirmId = null, Guid? accountId = null, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var trades = await db.Trades
            .AsNoTracking()
            .Where(t => t.Account!.UserId == userId)
            .Where(t => accountId == null || t.AccountId == accountId)
            .Where(t => propFirmId == null || t.Account!.PropFirmId == propFirmId)
            .OrderBy(t => t.ClosedAt)
            .Select(t => new { t.ClosedAt, NetPnL = t.GrossPnL - t.Commissions, t.RiskedAmount })
            .ToListAsync(ct);

        if (trades.Count == 0)
        {
            return new TradingKpis(0, 0, 0, null, 0m, 0m, 0m, null, 0m, 0m, null, null, null, 0m, 0, 0);
        }

        int total = trades.Count;
        var wins = trades.Where(t => t.NetPnL > 0).ToList();
        var losses = trades.Where(t => t.NetPnL < 0).ToList();

        decimal netPnL = trades.Sum(t => t.NetPnL);
        decimal grossProfit = wins.Sum(t => t.NetPnL);
        decimal grossLoss = Math.Abs(losses.Sum(t => t.NetPnL));

        double? winRate = (double)wins.Count / total;
        decimal avgWin = wins.Count > 0 ? grossProfit / wins.Count : 0m;
        decimal avgLoss = losses.Count > 0 ? grossLoss / losses.Count : 0m;
        double? profitFactor = grossLoss > 0 ? (double)(grossProfit / grossLoss) : null;
        double? payoffRatio = avgLoss > 0 ? (double)(avgWin / avgLoss) : null;

        double lossRate = 1 - winRate.Value;
        decimal? expectancy = (decimal)winRate.Value * avgWin - (decimal)lossRate * avgLoss;

        var rMultiples = trades.Where(t => t.RiskedAmount is > 0)
            .Select(t => (double)(t.NetPnL / t.RiskedAmount!.Value))
            .ToList();
        double? avgRMultiple = rMultiples.Count > 0 ? rMultiples.Average() : null;

        // Max drawdown sobre la curva de equity (peak-to-trough acumulado)
        decimal cumulative = 0m, peak = 0m, maxDrawdown = 0m;
        int currentLossStreak = 0, currentWinStreak = 0, maxLossStreak = 0, maxWinStreak = 0;
        foreach (var t in trades)
        {
            cumulative += t.NetPnL;
            peak = Math.Max(peak, cumulative);
            maxDrawdown = Math.Max(maxDrawdown, peak - cumulative);

            if (t.NetPnL > 0)
            {
                currentWinStreak++;
                currentLossStreak = 0;
            }
            else if (t.NetPnL < 0)
            {
                currentLossStreak++;
                currentWinStreak = 0;
            }
            else
            {
                currentWinStreak = 0;
                currentLossStreak = 0;
            }

            maxWinStreak = Math.Max(maxWinStreak, currentWinStreak);
            maxLossStreak = Math.Max(maxLossStreak, currentLossStreak);
        }

        return new TradingKpis(
            TotalTrades: total,
            WinningTrades: wins.Count,
            LosingTrades: losses.Count,
            WinRate: winRate,
            NetPnL: netPnL,
            GrossProfit: grossProfit,
            GrossLoss: grossLoss,
            ProfitFactor: profitFactor,
            AvgWin: avgWin,
            AvgLoss: avgLoss,
            PayoffRatio: payoffRatio,
            Expectancy: expectancy,
            AvgRMultiple: avgRMultiple,
            MaxDrawdown: maxDrawdown,
            MaxConsecutiveLosses: maxLossStreak,
            MaxConsecutiveWins: maxWinStreak);
    }

    public async Task<IReadOnlyList<MonthlyCashflowPoint>> GetMonthlyCashflowAsync(int months = 12, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var since = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-months));

        var costs = await db.AccountCosts.AsNoTracking()
            .Where(c => c.Account!.UserId == userId)
            .Where(c => c.PaidOn >= since)
            .Select(c => new { c.PaidOn, c.Amount })
            .ToListAsync(ct);

        var payouts = await db.Payouts.AsNoTracking()
            .Where(p => p.Account!.UserId == userId)
            .Where(p => (p.PaidOn ?? p.RequestedOn) >= since)
            .Select(p => new { Date = p.PaidOn ?? p.RequestedOn, p.AmountReceived })
            .ToListAsync(ct);

        var costsByMonth = costs.GroupBy(c => (c.PaidOn.Year, c.PaidOn.Month))
            .ToDictionary(g => g.Key, g => g.Sum(c => c.Amount));
        var payoutsByMonth = payouts.GroupBy(p => (p.Date.Year, p.Date.Month))
            .ToDictionary(g => g.Key, g => g.Sum(p => p.AmountReceived));

        var allKeys = costsByMonth.Keys.Concat(payoutsByMonth.Keys).Distinct()
            .OrderBy(k => k.Year).ThenBy(k => k.Month);

        return allKeys.Select(k =>
        {
            var c = costsByMonth.GetValueOrDefault(k, 0m);
            var p = payoutsByMonth.GetValueOrDefault(k, 0m);
            return new MonthlyCashflowPoint(k.Year, k.Month, c, p, p - c);
        }).ToList();
    }

    public async Task<IReadOnlyList<EquityCurvePoint>> GetEquityCurveAsync(Guid? accountId = null, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var trades = await db.Trades.AsNoTracking()
            .Where(t => t.Account!.UserId == userId)
            .Where(t => accountId == null || t.AccountId == accountId)
            .OrderBy(t => t.ClosedAt)
            .Select(t => new { t.ClosedAt, NetPnL = t.GrossPnL - t.Commissions })
            .ToListAsync(ct);

        var byDay = trades
            .GroupBy(t => DateOnly.FromDateTime(t.ClosedAt.Date))
            .OrderBy(g => g.Key)
            .Select(g => new { Date = g.Key, DayPnL = g.Sum(t => t.NetPnL) });

        var result = new List<EquityCurvePoint>();
        decimal cumulative = 0m;
        foreach (var day in byDay)
        {
            cumulative += day.DayPnL;
            result.Add(new EquityCurvePoint(day.Date, cumulative));
        }
        return result;
    }

    public async Task<PeriodKpis> GetPeriodKpisAsync(DateOnly start, DateOnly end, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var costs = await db.AccountCosts.AsNoTracking()
            .Where(c => c.Account!.UserId == userId)
            .Where(c => c.PaidOn >= start && c.PaidOn <= end)
            .Select(c => new { c.PaidOn, c.Amount })
            .ToListAsync(ct);

        var payouts = await db.Payouts.AsNoTracking()
            .Where(p => p.Account!.UserId == userId)
            .Where(p => (p.PaidOn ?? p.RequestedOn) >= start && (p.PaidOn ?? p.RequestedOn) <= end)
            .Select(p => new { Date = p.PaidOn ?? p.RequestedOn, p.AmountReceived })
            .ToListAsync(ct);

        var evaluationsPurchased = await db.TradingAccounts.AsNoTracking()
            .Where(a => a.UserId == userId)
            .Where(a => a.PurchasedOn >= start && a.PurchasedOn <= end)
            .CountAsync(ct);

        var totalCosts = costs.Sum(c => c.Amount);
        var totalPayouts = payouts.Sum(p => p.AmountReceived);

        var movements = costs.Select(c => (Date: c.PaidOn, Cost: c.Amount, Payout: 0m))
            .Concat(payouts.Select(p => (Date: p.Date, Cost: 0m, Payout: p.AmountReceived)))
            .ToList();

        return new PeriodKpis(
            Start: start,
            End: end,
            TotalCosts: totalCosts,
            TotalPayouts: totalPayouts,
            NetCashflow: totalPayouts - totalCosts,
            EvaluationsPurchased: evaluationsPurchased,
            Roi: totalCosts > 0 ? (double)((totalPayouts - totalCosts) / totalCosts) : null,
            EquityCurve: BuildEquityCurve(start, end, movements));
    }

    // Agrega los movimientos (costes/payouts) del rango en clamp(días, 7, 26) bins uniformes, con
    // el neto acumulado del periodo (no el histórico completo de la cuenta).
    private static IReadOnlyList<EquityBinPoint> BuildEquityCurve(DateOnly start, DateOnly end, IReadOnlyList<(DateOnly Date, decimal Cost, decimal Payout)> movements)
    {
        var totalDays = end.DayNumber - start.DayNumber + 1;
        var bins = Math.Clamp(totalDays, 7, 26);
        var result = new List<EquityBinPoint>(bins);
        decimal cumulative = 0m;

        for (var i = 0; i < bins; i++)
        {
            var binStart = start.AddDays((int)((long)totalDays * i / bins));
            var binEnd = i == bins - 1 ? end : start.AddDays((int)((long)totalDays * (i + 1) / bins) - 1);

            decimal dayCost = 0m, dayPayout = 0m;
            foreach (var m in movements)
            {
                if (m.Date >= binStart && m.Date <= binEnd)
                {
                    dayCost += m.Cost;
                    dayPayout += m.Payout;
                }
            }

            cumulative += dayPayout - dayCost;
            result.Add(new EquityBinPoint(binEnd, cumulative, dayCost, dayPayout));
        }

        return result;
    }

    public async Task<IReadOnlyList<TagPerformanceDto>> GetTagPerformanceAsync(CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var trades = await db.Trades.AsNoTracking()
            .Where(t => t.Account!.UserId == userId && t.Tags != null && t.Tags != "")
            .Select(t => new { NetPnL = t.GrossPnL - t.Commissions, t.Tags })
            .ToListAsync(ct);

        var pnlsByTag = new Dictionary<string, List<decimal>>(StringComparer.OrdinalIgnoreCase);
        foreach (var trade in trades)
        {
            // Un trade con varios tags ("breakout, news") cuenta en el rendimiento de cada uno.
            foreach (var tag in trade.Tags!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!pnlsByTag.TryGetValue(tag, out var pnls))
                {
                    pnls = [];
                    pnlsByTag[tag] = pnls;
                }
                pnls.Add(trade.NetPnL);
            }
        }

        return pnlsByTag.Select(kv =>
        {
            var pnls = kv.Value;
            var grossProfit = pnls.Where(p => p > 0).Sum();
            var grossLoss = Math.Abs(pnls.Where(p => p < 0).Sum());
            return new TagPerformanceDto(
                Tag: kv.Key,
                TotalTrades: pnls.Count,
                WinRate: (double)pnls.Count(p => p > 0) / pnls.Count,
                NetPnL: pnls.Sum(),
                ProfitFactor: grossLoss > 0 ? (double)(grossProfit / grossLoss) : null);
        })
        .OrderByDescending(d => d.NetPnL)
        .ToList();
    }

    public async Task<IReadOnlyList<FirmBusinessBreakdownDto>> GetFirmBusinessBreakdownAsync(CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var accounts = await db.TradingAccounts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => new
            {
                a.PropFirmId,
                FirmName = a.PropFirm!.Name,
                a.Stage,
                a.FundedOn,
                TotalCosts = a.Costs.Sum(c => (decimal?)c.Amount) ?? 0m,
                TotalPayouts = a.Payouts.Sum(p => (decimal?)p.AmountReceived) ?? 0m,
                FirstPayoutOn = a.Payouts
                    .Where(p => p.PaidOn != null)
                    .OrderBy(p => p.PaidOn)
                    .Select(p => (DateOnly?)p.PaidOn)
                    .FirstOrDefault(),
            })
            .ToListAsync(ct);

        return accounts
            .GroupBy(a => (a.PropFirmId, a.FirmName))
            .Select(g =>
            {
                var everFunded = g.Count(a => a.FundedOn is not null);
                var terminated = g.Count(a => a.FundedOn is not null || a.Stage is AccountStage.Failed or AccountStage.Expired);
                var totalCosts = g.Sum(a => a.TotalCosts);
                var totalPayouts = g.Sum(a => a.TotalPayouts);

                var daysToPayout = g
                    .Where(a => a.FundedOn is not null && a.FirstPayoutOn is not null)
                    .Select(a => (a.FirstPayoutOn!.Value.ToDateTime(TimeOnly.MinValue) - a.FundedOn!.Value.ToDateTime(TimeOnly.MinValue)).TotalDays)
                    .ToList();

                return new FirmBusinessBreakdownDto(
                    PropFirmId: g.Key.PropFirmId,
                    FirmName: g.Key.FirmName,
                    AccountsPurchased: g.Count(),
                    AccountsFunded: g.Count(a => a.Stage == AccountStage.Funded),
                    AccountsFailed: g.Count(a => a.Stage == AccountStage.Failed),
                    EvaluationsTerminated: terminated,
                    PassRate: terminated > 0 ? (double)everFunded / terminated : null,
                    TotalCosts: totalCosts,
                    TotalPayoutsReceived: totalPayouts,
                    NetCashflow: totalPayouts - totalCosts,
                    CostPerFundedAccount: everFunded > 0 ? totalCosts / everFunded : null,
                    AvgPayoutPerFundedAccount: everFunded > 0 ? totalPayouts / everFunded : null,
                    BusinessRoi: totalCosts > 0 ? (double)((totalPayouts - totalCosts) / totalCosts) : null,
                    AvgDaysFundedToFirstPayout: daysToPayout.Count > 0 ? daysToPayout.Average() : null);
            })
            .OrderByDescending(d => d.NetCashflow)
            .ToList();
    }

    public async Task<IReadOnlyList<TimeOfDayPerformancePoint>> GetTimeOfDayHeatmapAsync(CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var trades = await db.Trades.AsNoTracking()
            .Where(t => t.Account!.UserId == userId)
            .Select(t => new { t.OpenedAt, NetPnL = t.GrossPnL - t.Commissions })
            .ToListAsync(ct);

        return trades
            .GroupBy(t => (t.OpenedAt.DayOfWeek, Hour: t.OpenedAt.Hour))
            .Select(g =>
            {
                var pnls = g.Select(t => t.NetPnL).ToList();
                var wins = pnls.Count(p => p > 0);
                var winRate = (double)wins / pnls.Count;
                var avgWin = pnls.Where(p => p > 0).DefaultIfEmpty(0m).Average();
                var avgLoss = Math.Abs(pnls.Where(p => p < 0).DefaultIfEmpty(0m).Average());
                var expectancy = (decimal)winRate * avgWin - (decimal)(1 - winRate) * avgLoss;

                return new TimeOfDayPerformancePoint(g.Key.DayOfWeek, g.Key.Hour, pnls.Count, winRate, expectancy, pnls.Sum());
            })
            .OrderBy(p => p.DayOfWeek).ThenBy(p => p.Hour)
            .ToList();
    }

    public async Task<DurationAsymmetryDto> GetDurationAsymmetryAsync(CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var trades = await db.Trades.AsNoTracking()
            .Where(t => t.Account!.UserId == userId)
            .Select(t => new { t.OpenedAt, t.ClosedAt, NetPnL = t.GrossPnL - t.Commissions })
            .ToListAsync(ct);

        var wins = trades.Where(t => t.NetPnL > 0).Select(t => (t.ClosedAt - t.OpenedAt).TotalMinutes).ToList();
        var losses = trades.Where(t => t.NetPnL < 0).Select(t => (t.ClosedAt - t.OpenedAt).TotalMinutes).ToList();

        return new DurationAsymmetryDto(
            wins.Count > 0 ? wins.Average() : null,
            losses.Count > 0 ? losses.Average() : null,
            wins.Count,
            losses.Count);
    }

    public async Task<ExecutionQualityDto> GetExecutionQualityAsync(CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var trades = await db.Trades.AsNoTracking()
            .Where(t => t.Account!.UserId == userId)
            .Select(t => new
            {
                NetPnL = t.GrossPnL - t.Commissions,
                t.RiskedAmount,
                t.MaxAdverseExcursion,
                t.MaxFavorableExcursion,
            })
            .ToListAsync(ct);

        if (trades.Count == 0)
        {
            return new ExecutionQualityDto(0, 0, null, null, null);
        }

        var withData = trades.Where(t => t.MaxAdverseExcursion is not null || t.MaxFavorableExcursion is not null).ToList();
        var coverage = (double)withData.Count / trades.Count;

        var captureRatios = trades
            .Where(t => t.MaxFavorableExcursion is > 0)
            .Select(t => (double)(t.NetPnL / t.MaxFavorableExcursion!.Value))
            .ToList();

        var maeRs = trades
            .Where(t => t.RiskedAmount is > 0 && t.MaxAdverseExcursion is not null)
            .Select(t => (double)(t.MaxAdverseExcursion!.Value / t.RiskedAmount!.Value))
            .ToList();

        var mfeRs = trades
            .Where(t => t.RiskedAmount is > 0 && t.MaxFavorableExcursion is not null)
            .Select(t => (double)(t.MaxFavorableExcursion!.Value / t.RiskedAmount!.Value))
            .ToList();

        return new ExecutionQualityDto(
            withData.Count,
            coverage,
            captureRatios.Count > 0 ? captureRatios.Average() : null,
            maeRs.Count > 0 ? maeRs.Average() : null,
            mfeRs.Count > 0 ? mfeRs.Average() : null);
    }
}
