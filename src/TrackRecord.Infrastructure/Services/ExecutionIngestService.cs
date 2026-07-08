using Microsoft.EntityFrameworkCore;
using TrackRecord.Application.Abstractions;
using TrackRecord.Application.Dtos;
using TrackRecord.Domain.Entities;
using TrackRecord.Infrastructure.Persistence;

namespace TrackRecord.Infrastructure.Services;

public class ExecutionIngestService(
    IDbContextFactory<TrackRecordDbContext> dbFactory,
    ITradeRebuildService rebuildService) : IExecutionIngestService
{
    public async Task<IngestExecutionResult> IngestAsync(IngestExecutionRequest request, string userId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var accountId = await db.TradingAccounts
            .Where(a => a.UserId == userId && a.ExternalAccountId == request.AccountExternalId)
            .Select(a => (Guid?)a.Id)
            .SingleOrDefaultAsync(ct);

        if (accountId is null)
        {
            return new IngestExecutionResult(Inserted: false, AccountResolved: false, TradesRebuilt: 0);
        }

        var symbol = request.Symbol.ToUpperInvariant();

        // Idempotencia: (Source, ExternalId) es único. Si ya existe, no se reinserta ni se
        // vuelve a lanzar la reconstrucción (evita trabajo redundante en reintentos del AddOn).
        var alreadyExists = await db.Executions
            .AnyAsync(e => e.Source == request.Source && e.ExternalId == request.ExternalId, ct);

        if (alreadyExists)
        {
            return new IngestExecutionResult(Inserted: false, AccountResolved: true, TradesRebuilt: 0);
        }

        db.Executions.Add(new Execution
        {
            AccountId = accountId.Value,
            ExternalId = request.ExternalId,
            Source = request.Source,
            Symbol = symbol,
            Side = request.Side,
            Quantity = request.Quantity,
            Price = request.Price,
            ExecutedAt = request.ExecutedAt,
            Commission = request.Commission,
        });

        await db.SaveChangesAsync(ct);

        var tradesRebuilt = await rebuildService.RebuildAsync(accountId.Value, symbol, ct);

        return new IngestExecutionResult(Inserted: true, AccountResolved: true, TradesRebuilt: tradesRebuilt);
    }
}
