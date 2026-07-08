namespace TrackRecord.Application.Abstractions;

/// <summary>
/// Sincroniza fills desde fuentes push-pull (hoy: Tradovate) para todas las cuentas activas con
/// Feed = Tradovate. Las cuentas con Feed = NinjaTrader no requieren sincronización activa (los
/// fills llegan por push vía /api/ingest/ninjatrader/executions). Ver GUIA_IMPLEMENTACION.md §7.
/// </summary>
public interface ITradeSyncOrchestrator
{
    /// <summary>Sincroniza las cuentas activas (Evaluation/Funded) con Feed = Tradovate del usuario autenticado.</summary>
    Task<int> SyncAllAccountsAsync(CancellationToken ct = default);

    /// <summary>Como SyncAllAccountsAsync, pero para un usuario explícito — usado por el barrido en segundo plano.</summary>
    Task<int> SyncAllAccountsForUserAsync(string userId, CancellationToken ct = default);

    /// <summary>Sincroniza las cuentas Tradovate activas de TODOS los usuarios de la instancia (job periódico).</summary>
    Task<int> SyncAllUsersAsync(CancellationToken ct = default);

    /// <summary>Sincroniza una única cuenta (botón "Sincronizar ahora" en la UI).</summary>
    Task<int> SyncAccountAsync(Guid accountId, CancellationToken ct = default);
}
