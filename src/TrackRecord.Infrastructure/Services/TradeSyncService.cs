using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TrackRecord.Application.Abstractions;

namespace TrackRecord.Infrastructure.Services;

/// <summary>
/// Sincroniza periódicamente los fills de Tradovate en segundo plano (ver
/// GUIA_IMPLEMENTACION.md §7). El intervalo se configura con "Sync:IntervalMinutes"
/// (por defecto 10). Crea un scope nuevo en cada iteración porque ITradeSyncOrchestrator y sus
/// dependencias son Scoped, mientras que BackgroundService vive con ciclo de vida Singleton.
/// </summary>
public class TradeSyncService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<TradeSyncService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = configuration.GetValue("Sync:IntervalMinutes", 10);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));

        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<ITradeSyncOrchestrator>();
                var ingested = await orchestrator.SyncAllUsersAsync(stoppingToken);

                if (ingested > 0)
                {
                    logger.LogInformation("Sincronización de trades: {Count} fills nuevos ingestados.", ingested);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fallo en la sincronización periódica de trades.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
