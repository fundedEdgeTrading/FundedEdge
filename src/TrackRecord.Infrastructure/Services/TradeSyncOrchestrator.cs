using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TrackRecord.Application.Abstractions;
using TrackRecord.Application.Dtos;
using TrackRecord.Domain.Enums;
using TrackRecord.Infrastructure.Integrations.Tradovate;
using TrackRecord.Infrastructure.Persistence;

namespace TrackRecord.Infrastructure.Services;

public class TradeSyncOrchestrator(
    IDbContextFactory<TrackRecordDbContext> dbFactory,
    ITradovateClient tradovateClient,
    ITradovateCredentialStore credentialStore,
    IExecutionIngestService ingestService,
    ICurrentUserAccessor currentUser,
    IPlanService planService,
    ILogger<TradeSyncOrchestrator> logger) : ITradeSyncOrchestrator
{
    private static readonly TimeSpan SyncOverlap = TimeSpan.FromHours(1);

    /// <summary>
    /// Sincroniza todas las cuentas Tradovate del usuario actual (llamada interactiva desde
    /// /settings). Para el barrido global en segundo plano de TODOS los usuarios, ver
    /// TradeSyncService, que resuelve el propietario de cada cuenta directamente en base de datos.
    /// </summary>
    public async Task<int> SyncAllAccountsAsync(CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        return await SyncAllAccountsForUserAsync(userId, ct);
    }

    public async Task<int> SyncAllAccountsForUserAsync(string userId, CancellationToken ct = default)
    {
        var limits = await planService.GetLimitsAsync(userId, ct);
        if (!limits.AutoSyncEnabled)
        {
            logger.LogDebug("El plan del usuario {UserId} no incluye sincronización automática; se omite.", userId);
            return 0;
        }

        if (await credentialStore.GetCredentialsAsync(userId, ct) is null)
        {
            logger.LogDebug("Sin credenciales de Tradovate configuradas para el usuario {UserId}; se omite la sincronización.", userId);
            return 0;
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var accountIds = await db.TradingAccounts
            .Where(a => a.UserId == userId)
            .Where(a => a.Feed == DataFeedType.Tradovate)
            .Where(a => a.Stage == AccountStage.Evaluation || a.Stage == AccountStage.Funded)
            .Select(a => a.Id)
            .ToListAsync(ct);

        var totalIngested = 0;
        foreach (var accountId in accountIds)
        {
            totalIngested += await SyncAccountAsync(accountId, ct);
        }

        return totalIngested;
    }

    public async Task<int> SyncAllUsersAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var userIds = await db.TradingAccounts
            .Where(a => a.UserId != null && a.Feed == DataFeedType.Tradovate)
            .Where(a => a.Stage == AccountStage.Evaluation || a.Stage == AccountStage.Funded)
            .Select(a => a.UserId!)
            .Distinct()
            .ToListAsync(ct);

        var totalIngested = 0;
        foreach (var userId in userIds)
        {
            totalIngested += await SyncAllAccountsForUserAsync(userId, ct);
        }

        return totalIngested;
    }

    public async Task<int> SyncAccountAsync(Guid accountId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var account = await db.TradingAccounts.FindAsync([accountId], ct);
        if (account is null || account.UserId is null || account.Feed != DataFeedType.Tradovate || string.IsNullOrWhiteSpace(account.ExternalAccountId))
        {
            return 0;
        }

        if (!long.TryParse(account.ExternalAccountId, out var tradovateAccountId))
        {
            logger.LogWarning(
                "La cuenta {AccountId} tiene Feed=Tradovate pero ExternalAccountId '{ExternalAccountId}' no es numérico; se omite.",
                accountId, account.ExternalAccountId);
            return 0;
        }

        var userId = account.UserId;
        var since = await ComputeSinceWatermarkAsync(db, accountId, account.PurchasedOn, ct);

        IReadOnlyList<TradovateFill> fills;
        try
        {
            fills = await tradovateClient.GetFillsAsync(userId, tradovateAccountId, since, ct);
        }
        catch (TradovateApiException ex)
        {
            logger.LogError(ex, "Fallo consultando fills de Tradovate para la cuenta {AccountId}.", accountId);
            return 0;
        }

        var ingested = 0;
        foreach (var fill in fills)
        {
            var result = await ingestService.IngestAsync(
                new IngestExecutionRequest(
                    $"tv-{fill.Id}",
                    TradeSourceType.Tradovate,
                    account.ExternalAccountId,
                    fill.Symbol,
                    fill.Action == "Buy" ? OrderSide.Buy : OrderSide.Sell,
                    fill.Quantity,
                    fill.Price,
                    fill.ExecutedAt,
                    // Tradovate no siempre reporta la comisión en fill/list — queda en 0 hasta
                    // resolver el endpoint de comisiones (ver GUIA_IMPLEMENTACION.md Apéndice A).
                    Commission: 0m),
                userId,
                ct);

            if (result.Inserted) ingested++;
        }

        return ingested;
    }

    private static async Task<DateTimeOffset> ComputeSinceWatermarkAsync(TrackRecordDbContext db, Guid accountId, DateOnly purchasedOn, CancellationToken ct)
    {
        var lastExecutedAt = await db.Executions
            .Where(e => e.AccountId == accountId && e.Source == TradeSourceType.Tradovate)
            .OrderByDescending(e => e.ExecutedAt)
            .Select(e => (DateTimeOffset?)e.ExecutedAt)
            .FirstOrDefaultAsync(ct);

        // Primera sincronización: backfill desde la fecha de compra de la cuenta. En
        // sincronizaciones posteriores, se resta un solape de 1h para cubrir fills que hubieran
        // podido no estar aún visibles en Tradovate durante la sincronización anterior.
        return lastExecutedAt is null
            ? purchasedOn.ToDateTime(TimeOnly.MinValue)
            : lastExecutedAt.Value - SyncOverlap;
    }
}
