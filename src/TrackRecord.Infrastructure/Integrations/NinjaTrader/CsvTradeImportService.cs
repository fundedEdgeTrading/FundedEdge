using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using TrackRecord.Application.Abstractions;
using TrackRecord.Application.Dtos;
using TrackRecord.Domain.Enums;
using TrackRecord.Domain.Trades;
using TrackRecord.Infrastructure.Persistence;

namespace TrackRecord.Infrastructure.Integrations.NinjaTrader;

public class CsvTradeImportService(IDbContextFactory<TrackRecordDbContext> dbFactory, ICurrentUserAccessor currentUser) : ICsvTradeImportService
{
    private static readonly NinjaTraderCsvParser Parser = new();

    public async Task<CsvImportSummary> ImportAsync(Guid accountId, Stream csvStream, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        using var reader = new StreamReader(csvStream);
        var parseResult = Parser.Parse(reader);

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var owned = await db.TradingAccounts.AnyAsync(a => a.Id == accountId && a.UserId == userId, ct);
        if (!owned)
        {
            throw new KeyNotFoundException($"Cuenta {accountId} no encontrada.");
        }

        var imported = 0;
        var skipped = 0;

        foreach (var row in parseResult.Rows)
        {
            var rowKey = ComputeRowKey(row);
            var entryExternalId = $"csv-{rowKey}-entry";

            // Idempotencia: un hash determinista del contenido de la fila (no del Trade.Id, que
            // sería distinto en cada reimportación) permite reimportar el mismo CSV sin duplicar.
            var alreadyImported = await db.Executions
                .AnyAsync(e => e.Source == TradeSourceType.CsvImport && e.ExternalId == entryExternalId, ct);

            if (alreadyImported)
            {
                skipped++;
                continue;
            }

            var direction = row.MarketPosition == "Long" ? TradeDirection.Long : TradeDirection.Short;
            var trade = ManualTradeFactory.Create(
                accountId,
                row.Symbol,
                direction,
                row.Quantity,
                row.EntryPrice,
                row.ExitPrice,
                row.EntryTime,
                row.ExitTime,
                row.GrossPnL,
                row.Commission,
                riskedAmount: null,
                tags: null,
                notes: "Importado de CSV (NinjaTrader 8)",
                TradeSourceType.CsvImport,
                entryExternalId,
                $"csv-{rowKey}-exit");

            db.Trades.Add(trade);
            imported++;
        }

        await db.SaveChangesAsync(ct);

        var errors = parseResult.Errors.Select(e => $"Línea {e.LineNumber}: {e.Reason}").ToList();
        return new CsvImportSummary(imported, skipped, errors);
    }

    private static string ComputeRowKey(CsvTradeRow row)
    {
        var seed = $"{row.Symbol}|{row.EntryTime:O}|{row.ExitTime:O}|{row.Quantity}|{row.EntryPrice}|{row.ExitPrice}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        return Convert.ToHexString(hash)[..16];
    }
}
