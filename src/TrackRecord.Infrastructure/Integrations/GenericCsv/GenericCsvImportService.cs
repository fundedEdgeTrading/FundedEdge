using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using TrackRecord.Application.Abstractions;
using TrackRecord.Application.Dtos;
using TrackRecord.Domain.Enums;
using TrackRecord.Domain.Trades;
using TrackRecord.Infrastructure.Persistence;

namespace TrackRecord.Infrastructure.Integrations.GenericCsv;

public class GenericCsvImportService(
    IDbContextFactory<TrackRecordDbContext> dbFactory,
    ICurrentUserAccessor currentUser) : IGenericCsvImportService
{
    public Task<IReadOnlyList<string>> ReadHeadersAsync(Stream csvStream, CancellationToken ct = default)
    {
        // leaveOpen: true — el llamador reutiliza el mismo stream para ImportAsync después de
        // que el usuario confirme el mapeo de columnas; cerrarlo aquí lo dejaría inservible.
        using var reader = new StreamReader(csvStream, leaveOpen: true);
        return Task.FromResult(GenericCsvParser.ReadHeaders(reader));
    }

    public async Task<CsvImportSummary> ImportAsync(Guid accountId, Stream csvStream, GenericCsvColumnMapping mapping, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();

        using var reader = new StreamReader(csvStream, leaveOpen: true);
        var (columnIndex, rows) = GenericCsvParser.ReadAll(reader);

        int Resolve(string columnName) => columnIndex.TryGetValue(columnName, out var idx)
            ? idx
            : throw new InvalidOperationException($"La columna '{columnName}' no existe en el CSV.");

        var symbolCol = Resolve(mapping.Symbol);
        var directionCol = Resolve(mapping.Direction);
        var qtyCol = Resolve(mapping.Quantity);
        var entryPriceCol = Resolve(mapping.EntryPrice);
        var exitPriceCol = Resolve(mapping.ExitPrice);
        var openedAtCol = Resolve(mapping.OpenedAt);
        var closedAtCol = Resolve(mapping.ClosedAt);
        var grossPnLCol = Resolve(mapping.GrossPnL);
        var commissionsCol = mapping.Commissions is null ? -1 : Resolve(mapping.Commissions);
        var tagsCol = mapping.Tags is null ? -1 : Resolve(mapping.Tags);

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var owned = await db.TradingAccounts.AnyAsync(a => a.Id == accountId && a.UserId == userId, ct);
        if (!owned)
        {
            throw new KeyNotFoundException($"Cuenta {accountId} no encontrada.");
        }

        var imported = 0;
        var skipped = 0;
        var errors = new List<string>();

        foreach (var row in rows)
        {
            try
            {
                var fields = row.Fields;
                var symbol = Field(fields, symbolCol).Trim();
                var direction = ParseDirection(Field(fields, directionCol).Trim());
                var quantity = Math.Abs((int)decimal.Parse(Field(fields, qtyCol).Trim(), System.Globalization.CultureInfo.InvariantCulture));
                var entryPrice = GenericCsvParser.ParseDecimal(Field(fields, entryPriceCol));
                var exitPrice = GenericCsvParser.ParseDecimal(Field(fields, exitPriceCol));
                var openedAt = GenericCsvParser.ParseDate(Field(fields, openedAtCol));
                var closedAt = GenericCsvParser.ParseDate(Field(fields, closedAtCol));
                var grossPnL = GenericCsvParser.ParseDecimal(Field(fields, grossPnLCol));
                var commissions = commissionsCol >= 0 ? GenericCsvParser.ParseDecimal(Field(fields, commissionsCol)) : 0m;
                var tags = tagsCol >= 0 ? Field(fields, tagsCol).Trim() : null;

                var rowKey = ComputeRowKey(symbol, openedAt, closedAt, quantity, entryPrice, exitPrice);
                var entryExternalId = $"csv-generic-{rowKey}-entry";

                var alreadyImported = await db.Executions
                    .AnyAsync(e => e.Source == TradeSourceType.CsvImport && e.ExternalId == entryExternalId, ct);
                if (alreadyImported)
                {
                    skipped++;
                    continue;
                }

                var trade = ManualTradeFactory.Create(
                    accountId, symbol, direction, quantity, entryPrice, exitPrice, openedAt, closedAt,
                    grossPnL, commissions, riskedAmount: null,
                    tags: string.IsNullOrWhiteSpace(tags) ? null : tags,
                    notes: "Importado de CSV (mapeo genérico)",
                    TradeSourceType.CsvImport, entryExternalId, $"csv-generic-{rowKey}-exit");

                db.Trades.Add(trade);
                imported++;
            }
            catch (Exception ex) when (ex is FormatException or IndexOutOfRangeException or OverflowException)
            {
                errors.Add($"Línea {row.LineNumber}: {ex.Message}");
            }
        }

        await db.SaveChangesAsync(ct);
        return new CsvImportSummary(imported, skipped, errors);
    }

    private static TradeDirection ParseDirection(string raw)
    {
        var normalized = raw.Trim('"').Trim().ToUpperInvariant();
        if (normalized.StartsWith('S')) return TradeDirection.Short; // Short, Sell
        if (normalized.StartsWith('L') || normalized.StartsWith('B')) return TradeDirection.Long; // Long, Buy
        throw new FormatException($"No se pudo interpretar '{raw}' como dirección (long/short).");
    }

    private static string Field(IReadOnlyList<string> fields, int index) =>
        index < fields.Count ? fields[index] : throw new IndexOutOfRangeException($"Fila con menos columnas de las esperadas (columna {index}).");

    private static string ComputeRowKey(string symbol, DateTimeOffset openedAt, DateTimeOffset closedAt, int quantity, decimal entryPrice, decimal exitPrice)
    {
        var seed = $"{symbol}|{openedAt:O}|{closedAt:O}|{quantity}|{entryPrice}|{exitPrice}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        return Convert.ToHexString(hash)[..16];
    }
}
