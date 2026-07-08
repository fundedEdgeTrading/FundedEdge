using Markdig;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TrackRecord.Application.Abstractions;
using TrackRecord.Application.Ai;
using TrackRecord.Domain.Common;
using TrackRecord.Domain.Enums;
using TrackRecord.Infrastructure.Email;
using TrackRecord.Infrastructure.Persistence;

namespace TrackRecord.Infrastructure.Ai;

/// <summary>
/// Informe de análisis semanal programado (GUIA_IMPLEMENTACION.md §9/§12 Fase 3). Cada 6 horas
/// comprueba, para cada usuario registrado, si toca generar uno nuevo: IA configurada,
/// "Ai:WeeklyReportEnabled" = true (desactivado por defecto — genera coste de API), el usuario
/// tiene trades registrados, y su último informe automático tiene más de
/// "Ai:WeeklyReportIntervalDays" días (7 por defecto).
/// </summary>
public class WeeklyAiReportService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<WeeklyAiReportService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("Ai:WeeklyReportEnabled", false))
        {
            logger.LogDebug("Informe semanal de IA desactivado (Ai:WeeklyReportEnabled=false).");
            return;
        }

        var intervalDays = configuration.GetValue("Ai:WeeklyReportIntervalDays", 7);
        using var timer = new PeriodicTimer(CheckInterval);

        do
        {
            try
            {
                await GenerateForAllUsersAsync(intervalDays, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fallo generando el informe semanal de IA.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task GenerateForAllUsersAsync(int intervalDays, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TrackRecordDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Solo usuarios con al menos un trade registrado — no quemamos tokens en cuentas vacías.
        var userIds = await db.Trades
            .Where(t => t.Account!.UserId != null)
            .Select(t => t.Account!.UserId!)
            .Distinct()
            .ToListAsync(ct);

        foreach (var userId in userIds)
        {
            using var _ = CurrentUserContext.Impersonate(userId);
            await using var userScope = scopeFactory.CreateAsyncScope();
            await GenerateIfDueAsync(userScope, userId, intervalDays, ct);
        }
    }

    private async Task GenerateIfDueAsync(AsyncServiceScope scope, string userId, int intervalDays, CancellationToken ct)
    {
        var analyst = scope.ServiceProvider.GetRequiredService<ITradingAnalystService>();
        if (!analyst.IsConfigured)
        {
            return;
        }

        var planService = scope.ServiceProvider.GetRequiredService<IPlanService>();
        var limits = await planService.GetLimitsAsync(userId, ct);
        if (!limits.WeeklyAiReportEnabled)
        {
            return;
        }

        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TrackRecordDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var lastReportAt = await db.AiReports
            .Where(r => r.UserId == userId && r.Kind == AiReportKind.Analysis)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => (DateTimeOffset?)r.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (!IsDue(lastReportAt, DateTimeOffset.UtcNow, intervalDays))
        {
            return;
        }

        logger.LogInformation(
            "Generando informe semanal de IA para el usuario {UserId} (último: {Last}).",
            userId, lastReportAt?.ToString("u") ?? "nunca");

        try
        {
            var report = await analyst.GenerateAnalysisReportAsync(ct);
            await SendDigestEmailAsync(scope, db, userId, report.Content, ct);
        }
        catch (InvalidOperationException ex)
        {
            // El cupo de IA del plan puede haberse agotado ya con uso manual desde /ai en la
            // misma ventana; no debe abortar el resto del barrido de usuarios.
            logger.LogWarning("No se pudo generar el informe semanal de IA para el usuario {UserId}: {Message}", userId, ex.Message);
        }
    }

    /// <summary>
    /// Envía el informe recién generado por email (además de quedar guardado en /ai) — el gancho
    /// de retención semanal. Si falla el envío (SMTP caído, email vacío...) no aborta el barrido:
    /// el informe ya está generado y accesible desde la app igualmente.
    /// </summary>
    private async Task SendDigestEmailAsync(AsyncServiceScope scope, TrackRecordDbContext db, string userId, string reportMarkdown, CancellationToken ct)
    {
        try
        {
            var userEmail = await db.Users.Where(u => u.Id == userId).Select(u => u.Email).SingleOrDefaultAsync(ct);
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return;
            }

            var emailSender = scope.ServiceProvider.GetRequiredService<IAppEmailSender>();
            var html = $"""
                <p>Tu informe semanal de {Brand.Name}:</p>
                {Markdown.ToHtml(reportMarkdown)}
                <p style="color:#666;font-size:.85em">Consulta el historial completo en la sección "Análisis IA" de tu cuenta.</p>
                """;
            await emailSender.SendAsync(userEmail, $"Tu informe semanal de {Brand.Name}", html, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo enviar el email del informe semanal al usuario {UserId}.", userId);
        }
    }

    public static bool IsDue(DateTimeOffset? lastReportAt, DateTimeOffset now, int intervalDays) =>
        lastReportAt is null || now - lastReportAt.Value >= TimeSpan.FromDays(intervalDays);
}
