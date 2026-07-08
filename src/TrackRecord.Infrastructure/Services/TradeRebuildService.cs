using Microsoft.EntityFrameworkCore;
using TrackRecord.Application.Abstractions;
using TrackRecord.Domain.Enums;
using TrackRecord.Domain.Trades;
using TrackRecord.Infrastructure.Persistence;

namespace TrackRecord.Infrastructure.Services;

public class TradeRebuildService(IDbContextFactory<TrackRecordDbContext> dbFactory) : ITradeRebuildService
{
    public async Task<int> RebuildAsync(Guid accountId, string symbol, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var executions = await db.Executions
            .Where(e => e.AccountId == accountId && e.Symbol == symbol && e.Source != TradeSourceType.Manual)
            .ToListAsync(ct);

        if (executions.Count == 0)
        {
            return 0;
        }

        // Cualquier Trade previamente construido por TradeBuilder a partir de estas Executions
        // se descarta y se reconstruye desde cero: es la forma más simple de garantizar
        // consistencia tras un backfill que reordena o completa fills que faltaban.
        var oldTradeIds = executions.Where(e => e.TradeId is not null).Select(e => e.TradeId!.Value).Distinct().ToList();
        if (oldTradeIds.Count > 0)
        {
            var oldTrades = await db.Trades.Where(t => oldTradeIds.Contains(t.Id)).ToListAsync(ct);
            db.Trades.RemoveRange(oldTrades);
        }

        foreach (var exec in executions)
        {
            exec.TradeId = null;
            exec.Trade = null;
        }

        var instruments = await db.Instruments.AsNoTracking().ToListAsync(ct);
        var newTrades = TradeBuilder.Build(executions, instruments);

        db.Trades.AddRange(newTrades);
        await db.SaveChangesAsync(ct);

        return newTrades.Count;
    }
}
