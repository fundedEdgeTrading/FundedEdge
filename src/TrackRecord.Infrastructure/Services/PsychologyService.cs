using Microsoft.EntityFrameworkCore;
using TrackRecord.Application.Abstractions;
using TrackRecord.Application.Dtos;
using TrackRecord.Domain.Entities;
using TrackRecord.Domain.Enums;
using TrackRecord.Domain.Psychology;
using TrackRecord.Infrastructure.Persistence;

namespace TrackRecord.Infrastructure.Services;

/// <summary>
/// Implementación del diario emocional y del motor de diagnóstico (GUIA_PSICOLOGIA_TRADING.md).
/// Los detectores y el cálculo de métricas viven en Domain.Psychology (puros, testables); este
/// servicio solo se encarga de cargar/guardar los datos del usuario actual y aplanarlos al modelo
/// de entrada de esos cálculos.
/// </summary>
public class PsychologyService(
    IDbContextFactory<TrackRecordDbContext> dbFactory,
    ICurrentUserAccessor currentUser) : IPsychologyService
{
    public async Task<IReadOnlyList<PendingEmotionTradeDto>> GetPendingAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var (fromUtc, toUtc) = RangeBounds(from, to);

        return await db.Trades.AsNoTracking()
            .Where(t => t.Account!.UserId == userId)
            .Where(t => t.ClosedAt >= fromUtc && t.ClosedAt < toUtc)
            .Where(t => !db.TradeEmotionLogs.Any(l => l.TradeId == t.Id))
            .OrderBy(t => t.ClosedAt)
            .Select(t => new PendingEmotionTradeDto(t.Id, t.Symbol, t.OpenedAt, t.ClosedAt, t.GrossPnL - t.Commissions, t.RiskedAmount > 0 ? (t.GrossPnL - t.Commissions) / t.RiskedAmount : null))
            .ToListAsync(ct);
    }

    public async Task SaveTradeEmotionsAsync(SaveTradeEmotionsRequest request, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var ownsTrade = await db.Trades.AnyAsync(t => t.Id == request.TradeId && t.Account!.UserId == userId, ct);
        if (!ownsTrade)
        {
            throw new InvalidOperationException("El trade no existe o no pertenece al usuario actual.");
        }

        // Re-registrar sustituye el diario anterior de este trade en vez de acumular filas duplicadas.
        var existing = db.TradeEmotionLogs.Where(l => l.TradeId == request.TradeId);
        db.TradeEmotionLogs.RemoveRange(existing);

        var now = DateTimeOffset.UtcNow;
        foreach (var entry in request.BeforeEntry)
        {
            db.TradeEmotionLogs.Add(new TradeEmotionLog
            {
                TradeId = request.TradeId,
                Moment = EmotionMoment.BeforeEntry,
                Emotion = entry.Emotion,
                Intensity = entry.Intensity,
                Adherence = request.Adherence,
                WasImpulsive = request.WasImpulsive,
                Note = request.Note,
                LoggedAt = now,
            });
        }

        foreach (var entry in request.AfterExit)
        {
            db.TradeEmotionLogs.Add(new TradeEmotionLog
            {
                TradeId = request.TradeId,
                Moment = EmotionMoment.AfterExit,
                Emotion = entry.Emotion,
                Intensity = entry.Intensity,
                Adherence = request.Adherence,
                WasImpulsive = request.WasImpulsive,
                Note = request.Note,
                LoggedAt = now,
            });
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<DailyCheckInDto?> GetDailyCheckInAsync(DateOnly date, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        return await db.DailyMindsetCheckIns.AsNoTracking()
            .Where(c => c.UserId == userId && c.Date == date)
            .Select(c => new DailyCheckInDto(c.Date, c.SleepQuality, c.ExternalStress, c.PreMarketFocus, c.DominantPreMarketEmotion, c.Note))
            .FirstOrDefaultAsync(ct);
    }

    public async Task SaveDailyCheckInAsync(DailyCheckInDto dto, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var existing = await db.DailyMindsetCheckIns.FirstOrDefaultAsync(c => c.UserId == userId && c.Date == dto.Date, ct);
        if (existing is null)
        {
            db.DailyMindsetCheckIns.Add(new DailyMindsetCheckIn
            {
                UserId = userId,
                Date = dto.Date,
                SleepQuality = dto.SleepQuality,
                ExternalStress = dto.ExternalStress,
                PreMarketFocus = dto.PreMarketFocus,
                DominantPreMarketEmotion = dto.DominantPreMarketEmotion,
                Note = dto.Note,
            });
        }
        else
        {
            existing.SleepQuality = dto.SleepQuality;
            existing.ExternalStress = dto.ExternalStress;
            existing.PreMarketFocus = dto.PreMarketFocus;
            existing.DominantPreMarketEmotion = dto.DominantPreMarketEmotion;
            existing.Note = dto.Note;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<EmotionAnalyticsDto> GetAnalyticsAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var (trades, checkIns) = await LoadContextAsync(from, to, ct);

        return new EmotionAnalyticsDto(
            EmotionFrequency: BuildEmotionFrequency(trades),
            EmotionPerformance: BuildEmotionPerformance(trades),
            MoodCalendar: BuildMoodCalendar(trades, checkIns),
            DisciplineTrend: BuildDisciplineTrend(trades),
            EmotionalCapitalTrend: PsychMetricsCalculator.ComputeEmotionalCapitalTrend(trades, checkIns));
    }

    public async Task<PsychMetricsDto> GetMetricsAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var (trades, checkIns) = await LoadContextAsync(from, to, ct);
        var metrics = PsychMetricsCalculator.Compute(trades, checkIns);
        var insights = PsychDetectorEngine.Evaluate(trades, checkIns);

        var totalTradesInRange = await CountTradesInRangeAsync(from, to, ct);
        var coverage = totalTradesInRange > 0 ? (double)trades.Count / totalTradesInRange : 0;

        return new PsychMetricsDto(
            metrics.TiltIndex,
            metrics.DisciplineScore,
            metrics.EmotionalCostPerR,
            await ComputeJournalStreakAsync(ct),
            coverage,
            insights);
    }

    private async Task<int> CountTradesInRangeAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var (fromUtc, toUtc) = RangeBounds(from, to);
        return await db.Trades.AsNoTracking()
            .Where(t => t.Account!.UserId == userId && t.ClosedAt >= fromUtc && t.ClosedAt < toUtc)
            .CountAsync(ct);
    }

    /// <summary>Días consecutivos (terminando hoy o ayer) con check-in diario registrado.</summary>
    private async Task<int> ComputeJournalStreakAsync(CancellationToken ct)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var dates = await db.DailyMindsetCheckIns.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => c.Date)
            .OrderByDescending(d => d)
            .ToListAsync(ct);

        if (dates.Count == 0) return 0;

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        if (dates[0] != today && dates[0] != today.AddDays(-1)) return 0;

        var streak = 1;
        for (var i = 1; i < dates.Count; i++)
        {
            if (dates[i - 1].AddDays(-1) == dates[i]) streak++;
            else break;
        }
        return streak;
    }

    private async Task<(List<TradeWithEmotions> Trades, List<DailyMindset> CheckIns)> LoadContextAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var (fromUtc, toUtc) = RangeBounds(from, to);

        var trades = await db.Trades.AsNoTracking()
            .Where(t => t.Account!.UserId == userId && t.ClosedAt >= fromUtc && t.ClosedAt < toUtc)
            .Select(t => new
            {
                t.Id,
                t.OpenedAt,
                t.ClosedAt,
                NetPnL = t.GrossPnL - t.Commissions,
                RMultiple = t.RiskedAmount > 0 ? (t.GrossPnL - t.Commissions) / t.RiskedAmount : null,
                t.Quantity,
            })
            .ToListAsync(ct);

        var tradeIds = trades.Select(t => t.Id).ToList();
        var logs = await db.TradeEmotionLogs.AsNoTracking()
            .Where(l => tradeIds.Contains(l.TradeId))
            .ToListAsync(ct);

        var logsByTrade = logs.GroupBy(l => l.TradeId).ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<TradeWithEmotions>();
        foreach (var trade in trades)
        {
            if (!logsByTrade.TryGetValue(trade.Id, out var tradeLogs) || tradeLogs.Count == 0)
            {
                continue; // sin diario emocional: no participa en analítica ni detectores
            }

            var entry = tradeLogs.Where(l => l.Moment == EmotionMoment.BeforeEntry)
                .Select(l => new EmotionRating(l.Emotion, l.Intensity)).ToList();
            var exit = tradeLogs.Where(l => l.Moment == EmotionMoment.AfterExit)
                .Select(l => new EmotionRating(l.Emotion, l.Intensity)).ToList();
            var first = tradeLogs[0];

            result.Add(new TradeWithEmotions(
                trade.Id, trade.OpenedAt, trade.ClosedAt, trade.NetPnL, trade.RMultiple, trade.Quantity,
                entry, exit, first.Adherence, first.WasImpulsive, first.Note));
        }

        var checkIns = await db.DailyMindsetCheckIns.AsNoTracking()
            .Where(c => c.UserId == userId && c.Date >= from && c.Date <= to)
            .Select(c => new DailyMindset(c.Date, c.SleepQuality, c.ExternalStress, c.PreMarketFocus, c.DominantPreMarketEmotion))
            .ToListAsync(ct);

        return (result, checkIns);
    }

    private static IReadOnlyList<EmotionFrequencyPoint> BuildEmotionFrequency(IReadOnlyList<TradeWithEmotions> trades)
    {
        return trades
            .SelectMany(t => t.EntryEmotions.Concat(t.ExitEmotions).Select(e => (Week: WeekStart(t.OpenedAt), Rating: e)))
            .GroupBy(x => (x.Week, x.Rating.Emotion))
            .Select(g => new EmotionFrequencyPoint(g.Key.Week, g.Key.Emotion, g.Count(), g.Average(x => x.Rating.Intensity)))
            .OrderBy(p => p.WeekStart).ThenBy(p => p.Emotion)
            .ToList();
    }

    private static IReadOnlyList<EmotionPerformancePoint> BuildEmotionPerformance(IReadOnlyList<TradeWithEmotions> trades)
    {
        return trades
            .SelectMany(t => t.EntryEmotions.Select(e => e.Emotion).Distinct().Select(emotion => (Emotion: emotion, Trade: t)))
            .GroupBy(x => x.Emotion)
            .Select(g =>
            {
                var group = g.Select(x => x.Trade).ToList();
                var withR = group.Where(t => t.RMultiple is not null).ToList();
                return new EmotionPerformancePoint(
                    g.Key,
                    group.Count,
                    (double)group.Count(t => t.IsWin) / group.Count,
                    withR.Count > 0 ? withR.Average(t => t.RMultiple!.Value) : null,
                    group.Sum(t => t.NetPnL));
            })
            .OrderByDescending(p => p.TradeCount)
            .ToList();
    }

    private static IReadOnlyList<MoodCalendarDay> BuildMoodCalendar(IReadOnlyList<TradeWithEmotions> trades, IReadOnlyList<DailyMindset> checkIns)
    {
        var valenceByDay = PsychDetectorEngine.DailyValence(trades, checkIns).ToDictionary(d => d.Date, d => d.Valence);
        var pnlByDay = trades.GroupBy(t => DateOnly.FromDateTime(t.OpenedAt.Date)).ToDictionary(g => g.Key, g => g.Sum(t => t.NetPnL));
        var checkInDays = checkIns.Select(c => c.Date).ToHashSet();

        var allDays = valenceByDay.Keys.Concat(pnlByDay.Keys).Distinct().OrderBy(d => d);
        return allDays
            .Select(d => new MoodCalendarDay(d, valenceByDay.GetValueOrDefault(d, 0), pnlByDay.GetValueOrDefault(d, 0m), checkInDays.Contains(d)))
            .ToList();
    }

    private static IReadOnlyList<DisciplineTrendPoint> BuildDisciplineTrend(IReadOnlyList<TradeWithEmotions> trades)
    {
        return trades
            .GroupBy(t => WeekStart(t.OpenedAt))
            .Select(g => new DisciplineTrendPoint(g.Key, g.Count(t => t.Adherence == PlanAdherence.FollowedPlan) / (double)g.Count()))
            .OrderBy(p => p.WeekStart)
            .ToList();
    }

    private static DateOnly WeekStart(DateTimeOffset date)
    {
        var d = DateOnly.FromDateTime(date.Date);
        var diff = ((int)d.DayOfWeek + 6) % 7; // lunes = inicio de semana
        return d.AddDays(-diff);
    }

    private static (DateTimeOffset From, DateTimeOffset To) RangeBounds(DateOnly from, DateOnly to) =>
        (new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
         new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
}
