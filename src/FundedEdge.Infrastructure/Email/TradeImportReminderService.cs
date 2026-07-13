using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FundedEdge.Domain.Common;
using FundedEdge.Domain.Enums;
using FundedEdge.Infrastructure.Persistence;
using FundedEdge.Infrastructure.Settings;

namespace FundedEdge.Infrastructure.Email;

/// <summary>
/// Recordatorio de importación (PLAN_IMPLEMENTACION_MERCADO.md M1.3): si un usuario con cuentas
/// activas lleva más de "Import:ReminderAfterDays" días (7 por defecto) sin registrar trades,
/// le envía un email recordándole importar su CSV. Activado por defecto ("Import:ReminderEnabled")
/// porque sin SMTP configurado el envío es un no-op; como mucho envía uno por usuario por ventana.
/// La fecha del último recordatorio se persiste en IntegrationSettings para no repetir tras un
/// reinicio.
/// </summary>
public class TradeImportReminderService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<TradeImportReminderService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(12);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("Import:ReminderEnabled", true))
        {
            logger.LogDebug("Recordatorio de importación desactivado (Import:ReminderEnabled=false).");
            return;
        }

        var afterDays = configuration.GetValue("Import:ReminderAfterDays", 7);
        using var timer = new PeriodicTimer(CheckInterval);
        do
        {
            try
            {
                await RemindAllUsersAsync(afterDays, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fallo en el barrido de recordatorios de importación.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RemindAllUsersAsync(int afterDays, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<FundedEdgeDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Solo usuarios con alguna cuenta activa Y algún trade histórico: quien nunca ha
        // importado nada está en onboarding, no "con la importación abandonada".
        var candidates = await db.Trades.AsNoTracking()
            .Where(t => t.Account!.UserId != null)
            .GroupBy(t => t.Account!.UserId!)
            .Select(g => new { UserId = g.Key, LastTradeAt = g.Max(t => t.ClosedAt) })
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        foreach (var candidate in candidates)
        {
            var hasActiveAccounts = await db.TradingAccounts.AsNoTracking()
                .AnyAsync(a => a.UserId == candidate.UserId
                    && (a.Stage == AccountStage.Evaluation || a.Stage == AccountStage.Funded), ct);
            if (!hasActiveAccounts) continue;

            await RemindIfDueAsync(candidate.UserId, candidate.LastTradeAt, now, afterDays, ct);
        }
    }

    private async Task RemindIfDueAsync(string userId, DateTimeOffset lastTradeAt, DateTimeOffset now, int afterDays, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var settings = scope.ServiceProvider.GetRequiredService<IIntegrationSettingsStore>();

        var key = $"{userId}:Import:LastReminderAt";
        DateTimeOffset? lastReminderAt = DateTimeOffset.TryParse(await settings.GetAsync(key, ct), out var parsed)
            ? parsed
            : null;

        if (!IsReminderDue(lastTradeAt, lastReminderAt, now, afterDays)) return;

        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<FundedEdgeDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var userEmail = await db.Users.Where(u => u.Id == userId).Select(u => u.Email).SingleOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(userEmail)) return;

        var daysSince = (int)(now - lastTradeAt).TotalDays;
        try
        {
            var emailSender = scope.ServiceProvider.GetRequiredService<IAppEmailSender>();
            var html = $"""
                <p>Llevas {daysSince} días sin registrar trades en {Brand.Name} y tienes cuentas activas.</p>
                <p>Importa el CSV de tu plataforma (Tradovate / NinjaTrader 8) desde la ficha de cada
                cuenta para que tus KPIs, el módulo de riesgo y el semáforo de reglas sigan reflejando
                tu operativa real.</p>
                <p style="color:#666;font-size:.85em">Puedes desactivar este recordatorio pidiéndoselo a tu administrador (Import:ReminderEnabled).</p>
                """;
            await emailSender.SendAsync(userEmail, $"¿Trades sin importar? — {Brand.Name}", html, ct);
            await settings.SetAsync(key, now.ToString("o"), ct);
            logger.LogInformation("Recordatorio de importación enviado al usuario {UserId} ({Days} días sin trades).", userId, daysSince);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo enviar el recordatorio de importación al usuario {UserId}.", userId);
        }
    }

    /// <summary>
    /// Toca recordar si la última actividad supera la ventana y no se ha recordado ya desde
    /// entonces (ni dentro de la misma ventana, para no insistir cada barrido).
    /// </summary>
    public static bool IsReminderDue(DateTimeOffset lastTradeAt, DateTimeOffset? lastReminderAt, DateTimeOffset now, int afterDays)
    {
        if (now - lastTradeAt < TimeSpan.FromDays(afterDays)) return false;
        if (lastReminderAt is null) return true;
        return lastReminderAt.Value < lastTradeAt || now - lastReminderAt.Value >= TimeSpan.FromDays(afterDays);
    }
}
