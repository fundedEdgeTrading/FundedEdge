using Microsoft.EntityFrameworkCore;
using TrackRecord.Application.Abstractions;
using TrackRecord.Domain.Enums;
using TrackRecord.Infrastructure.Persistence;

namespace TrackRecord.Infrastructure.Services;

public class PlanService(
    IDbContextFactory<TrackRecordDbContext> dbFactory,
    ICurrentUserAccessor currentUser) : IPlanService
{
    public async Task<PlanTier> GetTierAsync(string? userId = null, CancellationToken ct = default)
    {
        var id = userId ?? await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var user = await db.Users.AsNoTracking()
            .Select(u => new { u.Id, u.PlanTier, u.TrialEndsAt })
            .SingleOrDefaultAsync(u => u.Id == id, ct);

        if (user is null) return PlanTier.Starter;

        // El trial solo "levanta" a un usuario Starter a Pro; si ya pagó un plan concreto
        // (o downgradeó explícitamente), su PlanTier persistido manda.
        if (user.PlanTier == PlanTier.Starter && user.TrialEndsAt is { } trialEndsAt && trialEndsAt > DateTimeOffset.UtcNow)
        {
            return PlanTier.Pro;
        }

        return user.PlanTier;
    }

    public async Task<PlanLimits> GetLimitsAsync(string? userId = null, CancellationToken ct = default) =>
        PlanLimits.For(await GetTierAsync(userId, ct));

    public async Task<bool> CanCreateAccountAsync(CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        var limits = await GetLimitsAsync(userId, ct);
        if (limits.MaxActiveAccounts is null) return true;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var activeCount = await db.TradingAccounts.CountAsync(a =>
            a.UserId == userId &&
            a.Stage != AccountStage.Failed && a.Stage != AccountStage.Withdrawn && a.Stage != AccountStage.Expired,
            ct);

        return activeCount < limits.MaxActiveAccounts.Value;
    }

    public async Task<AiAllowance> GetAiAllowanceAsync(CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        var limits = await GetLimitsAsync(userId, ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var now = DateTimeOffset.UtcNow;
        var reportWindowStart = now.AddDays(-limits.AiReportWindowDays);
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var dayStart = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);

        var reportTimestamps = await db.AiReports
            .Where(r => r.UserId == userId && r.Kind == AiReportKind.Analysis && r.CreatedAt >= reportWindowStart)
            .OrderBy(r => r.CreatedAt)
            .Select(r => r.CreatedAt)
            .ToListAsync(ct);

        var questionsUsed = await db.AiReports.CountAsync(r =>
            r.UserId == userId && r.Kind == AiReportKind.AdHocQuestion && r.CreatedAt >= monthStart, ct);

        // Tope anti-abuso diario: se aplica siempre, también en Elite (no se comunica en la UI).
        var dailyUsed = await db.AiReports.CountAsync(r => r.UserId == userId && r.CreatedAt >= dayStart, ct);
        var underDailyCap = dailyUsed < limits.AiDailyHardCap;

        var reportsUsed = reportTimestamps.Count;
        var canGenerateReport = underDailyCap && reportsUsed < limits.AiReportsPerWindow;
        var canAskQuestion = underDailyCap && (limits.AiQuestionsPerMonth is null || questionsUsed < limits.AiQuestionsPerMonth.Value);

        var windowResetsAt = reportTimestamps.Count > 0
            ? reportTimestamps[0].AddDays(limits.AiReportWindowDays)
            : now;

        return new AiAllowance(canGenerateReport, canAskQuestion, reportsUsed, limits.AiReportsPerWindow, questionsUsed, limits.AiQuestionsPerMonth, windowResetsAt);
    }
}
