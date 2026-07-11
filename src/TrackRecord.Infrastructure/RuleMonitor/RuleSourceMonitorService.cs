using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TrackRecord.Application.Abstractions;
using TrackRecord.Infrastructure.Persistence;

namespace TrackRecord.Infrastructure.RuleMonitor;

/// <summary>
/// Job diario del monitor de reglas (fase 1 de INVESTIGACION_AUTOMATIZACION_REGLAS.md): recorre
/// las fuentes habilitadas y delega en <see cref="RuleSourceChecker"/> la detección de cambios.
/// Las reglas de las firmas no cambian intradía, así que un barrido cada 24 h es suficiente y el
/// coste en régimen estacionario es casi nulo (solo fetch + hash). Desactivado por defecto
/// (RuleMonitor:Enabled=false).
/// </summary>
public class RuleSourceMonitorService(
    IServiceScopeFactory scopeFactory,
    RuleMonitorOptions options,
    ILogger<RuleSourceMonitorService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled)
        {
            logger.LogDebug("Monitor de reglas desactivado (RuleMonitor:Enabled=false).");
            return;
        }

        using var timer = new PeriodicTimer(CheckInterval);
        do
        {
            try
            {
                await CheckAllSourcesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fallo en el barrido del monitor de reglas.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CheckAllSourcesAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TrackRecordDbContext>>();

        List<Guid> sourceIds;
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            sourceIds = await db.RuleSources.AsNoTracking()
                .Where(s => s.IsEnabled)
                .Select(s => s.Id)
                .ToListAsync(ct);
        }

        if (sourceIds.Count == 0) return;
        logger.LogInformation("Monitor de reglas: comprobando {Count} fuentes.", sourceIds.Count);

        var checker = scope.ServiceProvider.GetRequiredService<IRuleSourceChecker>();
        foreach (var id in sourceIds)
        {
            try
            {
                await checker.CheckAsync(id, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Una fuente rota (borrada a mitad de barrido, error inesperado) no frena el resto.
                logger.LogError(ex, "Fallo comprobando la fuente {SourceId}.", id);
            }
        }
    }
}
