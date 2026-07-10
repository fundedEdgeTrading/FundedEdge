using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using TrackRecord.Application.Abstractions;
using TrackRecord.Application.Dtos;
using TrackRecord.Domain.Enums;
using TrackRecord.Domain.Trades;
using TrackRecord.Infrastructure.Persistence;

namespace TrackRecord.Infrastructure.Integrations.Csv;

/// <summary>
/// Importador unificado: acepta indistintamente el CSV de rendimiento de Tradovate y el export
/// de "Trade Performance" de NinjaTrader 8. El formato se detecta automáticamente por las
/// cabeceras de la primera línea y cada parser lo normaliza al mismo <see cref="CsvTradeRow"/>,
/// de modo que el resto de la aplicación (KPIs, reglas, equity, MAE/MFE…) no distingue el origen.
/// </summary>
public class CsvTradeImportService(IDbContextFactory<TrackRecordDbContext> dbFactory, ICurrentUserAccessor currentUser) : ICsvTradeImportService
{
    private static readonly NinjaTraderCsvParser NinjaTraderParser = new();
    private static readonly TradovateCsvParser TradovateParser = new();

    public async Task<CsvImportSummary> ImportAsync(Guid accountId, Stream csvStream, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();

        // Se materializa el contenido para poder inspeccionar la cabecera y después reparsear
        // desde el principio con el parser elegido (los archivos están limitados a 10 MB en la UI).
        string content;
        using (var reader = new StreamReader(csvStream, leaveOpen: true))
        {
            content = await reader.ReadToEndAsync(ct);
        }

        var (parseResult, sourceLabel) = ParseAutodetecting(content);

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
                notes: $"Importado de CSV ({sourceLabel})",
                TradeSourceType.CsvImport,
                entryExternalId,
                $"csv-{rowKey}-exit",
                row.MaxAdverseExcursion,
                row.MaxFavorableExcursion);

            db.Trades.Add(trade);
            imported++;
        }

        await db.SaveChangesAsync(ct);

        var errors = parseResult.Errors.Select(e => $"Línea {e.LineNumber}: {e.Reason}").ToList();
        return new CsvImportSummary(imported, skipped, errors);
    }

    private static (CsvParseResult Result, string SourceLabel) ParseAutodetecting(string content)
    {
        using var headerReader = new StringReader(content);
        var headerLine = headerReader.ReadLine();
        List<string> headers = headerLine is null ? [] : CsvLineSplitter.Split(headerLine);

        if (TradovateCsvParser.LooksLikeHeader(headers))
        {
            using var reader = new StringReader(content);
            return (TradovateParser.Parse(reader), "Tradovate");
        }

        if (NinjaTraderCsvParser.LooksLikeHeader(headers))
        {
            using var reader = new StringReader(content);
            return (NinjaTraderParser.Parse(reader), "NinjaTrader 8");
        }

        return (new CsvParseResult([], [new CsvParseError(1, headerLine ?? "",
            "Formato no reconocido: no parece un export de Tradovate (Reports → Performance) ni de NinjaTrader 8 (Trade Performance → Trades). " +
            "Si el archivo procede de otra plataforma, usa la importación con mapeo de columnas.")]), "desconocido");
    }

    private static string ComputeRowKey(CsvTradeRow row)
    {
        var seed = $"{row.Symbol}|{row.EntryTime:O}|{row.ExitTime:O}|{row.Quantity}|{row.EntryPrice}|{row.ExitPrice}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        return Convert.ToHexString(hash)[..16];
    }
}
