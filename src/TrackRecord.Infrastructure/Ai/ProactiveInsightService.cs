using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TrackRecord.Application.Abstractions;
using TrackRecord.Application.Ai;
using TrackRecord.Domain.Enums;
using TrackRecord.Infrastructure.Persistence;

namespace TrackRecord.Infrastructure.Ai;

/// <summary>
/// IA proactiva por eventos (GUIA_FUNCIONALIDADES_PROPUESTAS.md §4.1): barre los usuarios y
/// dispara un mini-informe cuando detecta (1) racha de 3+ días perdedores, (2) una cuenta a menos
/// del 20% de su colchón de drawdown, o (3) el primer payout cobrado. Desactivado por defecto
/// (mismo criterio que WeeklyAiReportService: genera coste de API).
/// </summary>
public class ProactiveInsightService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<ProactiveInsightService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);
    private static readonly TimeSpan LosingStreakCooldown = TimeSpan.FromHours(24);
    private static readonly TimeSpan DrawdownCooldown = TimeSpan.FromDays(3);
    private const int LosingStreakThreshold = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("Ai:ProactiveInsightsEnabled", false))
        {
            logger.LogDebug("IA proactiva por eventos desactivada (Ai:ProactiveInsightsEnabled=false).");
            return;
        }

        using var timer = new PeriodicTimer(CheckInterval);
        do
        {
            try
            {
                await ScanAllUsersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fallo en el barrido de IA proactiva por eventos.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ScanAllUsersAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TrackRecordDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var userIds = await db.Trades
            .Where(t => t.Account!.UserId != null)
            .Select(t => t.Account!.UserId!)
            .Distinct()
            .ToListAsync(ct);

        foreach (var userId in userIds)
        {
            using var _ = CurrentUserContext.Impersonate(userId);
            await using var userScope = scopeFactory.CreateAsyncScope();
            await CheckUserAsync(userScope, userId, ct);
        }
    }

    private async Task CheckUserAsync(AsyncServiceScope scope, string userId, CancellationToken ct)
    {
        var analyst = scope.ServiceProvider.GetRequiredService<ITradingAnalystService>();
        if (!analyst.IsConfigured) return;

        var planService = scope.ServiceProvider.GetRequiredService<IPlanService>();
        var limits = await planService.GetLimitsAsync(userId, ct);
        if (!limits.WeeklyAiReportEnabled) return; // reutiliza el mismo flag de plan que el informe semanal

        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TrackRecordDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        await CheckLosingStreakAsync(db, analyst, userId, ct);
        await CheckDrawdownRiskAsync(scope, db, analyst, userId, ct);
        await CheckFirstPayoutAsync(db, analyst, userId, ct);
    }

    private async Task CheckLosingStreakAsync(TrackRecordDbContext db, ITradingAnalystService analyst, string userId, CancellationToken ct)
    {
        var recentDays = await db.Trades.AsNoTracking()
            .Where(t => t.Account!.UserId == userId)
            .GroupBy(t => t.ClosedAt.Date)
            .Select(g => new { Day = g.Key, NetPnL = g.Sum(t => t.GrossPnL - t.Commissions) })
            .OrderByDescending(g => g.Day)
            .Take(10)
            .ToListAsync(ct);

        var streak = 0;
        foreach (var day in recentDays)
        {
            if (day.NetPnL >= 0) break;
            streak++;
        }
        if (streak < LosingStreakThreshold) return;

        if (await WasRecentlyGeneratedAsync(db, userId, AiReportKind.LosingStreakAlert, LosingStreakCooldown, ct)) return;

        await TryGenerateAsync(analyst, userId, AiReportKind.LosingStreakAlert,
            $"Racha de {streak} días consecutivos con resultado neto negativo.", ct);
    }

    private async Task CheckDrawdownRiskAsync(AsyncServiceScope scope, TrackRecordDbContext db, ITradingAnalystService analyst, string userId, CancellationToken ct)
    {
        var riskService = scope.ServiceProvider.GetRequiredService<IRiskAnalysisService>();
        var alerts = await riskService.GetDrawdownAlertsAsync(ct);
        if (alerts.Count == 0) return;

        if (await WasRecentlyGeneratedAsync(db, userId, AiReportKind.DrawdownRiskAlert, DrawdownCooldown, ct)) return;

        var description = string.Join("; ", alerts.Select(a =>
            $"{a.AccountDisplayName}: {a.ConsumedFraction:P0} del drawdown consumido, quedan {a.RemainingBuffer:0}"));

        await TryGenerateAsync(analyst, userId, AiReportKind.DrawdownRiskAlert, description, ct);
    }

    private async Task CheckFirstPayoutAsync(TrackRecordDbContext db, ITradingAnalystService analyst, string userId, CancellationToken ct)
    {
        var paidPayoutsCount = await db.Payouts.AsNoTracking()
            .Where(p => p.Account!.UserId == userId && p.Status == PayoutStatus.Paid)
            .CountAsync(ct);
        if (paidPayoutsCount != 1) return; // solo se dispara justo al cobrar el primero, no en cada barrido posterior

        var alreadyNotified = await db.AiReports.AsNoTracking()
            .AnyAsync(r => r.UserId == userId && r.Kind == AiReportKind.FirstPayoutMilestone, ct);
        if (alreadyNotified) return;

        await TryGenerateAsync(analyst, userId, AiReportKind.FirstPayoutMilestone, "Primer payout cobrado.", ct);
    }

    private async Task<bool> WasRecentlyGeneratedAsync(TrackRecordDbContext db, string userId, AiReportKind kind, TimeSpan cooldown, CancellationToken ct)
    {
        var lastGeneratedAt = await db.AiReports.AsNoTracking()
            .Where(r => r.UserId == userId && r.Kind == kind)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => (DateTimeOffset?)r.CreatedAt)
            .FirstOrDefaultAsync(ct);

        return lastGeneratedAt is not null && DateTimeOffset.UtcNow - lastGeneratedAt.Value < cooldown;
    }

    private async Task TryGenerateAsync(ITradingAnalystService analyst, string userId, AiReportKind kind, string eventContext, CancellationToken ct)
    {
        try
        {
            logger.LogInformation("Generando informe proactivo {Kind} para el usuario {UserId}.", kind, userId);
            await analyst.GenerateEventReportAsync(kind, eventContext, ct);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("No se pudo generar el informe proactivo {Kind} para el usuario {UserId}: {Message}", kind, userId, ex.Message);
        }
    }
}
