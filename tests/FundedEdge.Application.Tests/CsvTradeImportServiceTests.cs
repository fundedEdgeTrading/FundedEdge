using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using FundedEdge.Domain.Entities;
using FundedEdge.Domain.Enums;
using FundedEdge.Infrastructure.Integrations.Csv;

namespace FundedEdge.Application.Tests;

public class NinjaTraderCsvParserTests
{
    private const string SampleCsv =
        "Instrument,Account,Market pos.,Qty,Entry price,Exit price,Entry time,Exit time,Profit,Commission\n" +
        "ESH6,Sim101,Long,1,5000.00,5010.00,2026-01-05 14:00:00,2026-01-05 14:05:00,497.50,2.50\n" +
        "MESH6,Sim101,Short,2,5020.00,5015.00,2026-01-05 15:00:00,2026-01-05 15:10:00,11.50,0.50\n";

    [Fact]
    public void Parse_ValidCsv_ReturnsExpectedRows()
    {
        var parser = new NinjaTraderCsvParser();
        using var reader = new StringReader(SampleCsv);

        var result = parser.Parse(reader, CultureInfo.InvariantCulture);

        Assert.Empty(result.Errors);
        Assert.Equal(2, result.Rows.Count);

        var first = result.Rows[0];
        Assert.Equal("ESH6", first.Symbol);
        Assert.Equal("Long", first.MarketPosition);
        Assert.Equal(1, first.Quantity);
        Assert.Equal(5000.00m, first.EntryPrice);
        Assert.Equal(5010.00m, first.ExitPrice);
        Assert.Equal(2.50m, first.Commission);
        Assert.Equal(500.00m, first.GrossPnL); // Profit (497.50, neto) + Commission (2.50) = bruto
        Assert.Null(first.MaxAdverseExcursion); // sin columnas MAE/MFE en este export
    }

    [Fact]
    public void Parse_WithMaeMfeColumns_MapsExcursionData()
    {
        const string csv =
            "Instrument,Market pos.,Qty,Entry price,Exit price,Entry time,Exit time,Profit,Commission,MAE,MFE\n" +
            "MNQH6,Long,1,21000.00,21010.00,2026-01-05 14:00:00,2026-01-05 14:05:00,20.00,0.74,($15.00),$32.50\n";

        var parser = new NinjaTraderCsvParser();
        using var reader = new StringReader(csv);

        var result = parser.Parse(reader, CultureInfo.InvariantCulture);

        Assert.Empty(result.Errors);
        var row = Assert.Single(result.Rows);
        Assert.Equal(15.00m, row.MaxAdverseExcursion); // MAE se normaliza a magnitud positiva
        Assert.Equal(32.50m, row.MaxFavorableExcursion);
    }

    [Fact]
    public void Parse_EuropeanDecimalCommaFormat_DoesNotInflateAmountsBy100()
    {
        // Export en formato europeo (coma decimal, ; como delimitador) — el bug reportado
        // interpretaba "-102,40" como -10240 al tratar la coma como separador de miles.
        const string csv =
            "Instrument;Market pos.;Qty;Entry price;Exit price;Entry time;Exit time;Profit;Commission;MAE;MFE\n" +
            "ESH6;Long;1;5000,00;5010,00;2026-01-05 14:00:00;2026-01-05 14:05:00;-102,40;2,50;($15,00);$32,50\n";

        var parser = new NinjaTraderCsvParser();
        using var reader = new StringReader(csv);

        var result = parser.Parse(reader, CultureInfo.InvariantCulture);

        Assert.Empty(result.Errors);
        var row = Assert.Single(result.Rows);
        Assert.Equal(5000.00m, row.EntryPrice);
        Assert.Equal(5010.00m, row.ExitPrice);
        Assert.Equal(2.50m, row.Commission);
        Assert.Equal(-99.90m, row.GrossPnL); // Profit (-102.40) + Commission (2.50)
        Assert.Equal(15.00m, row.MaxAdverseExcursion);
        Assert.Equal(32.50m, row.MaxFavorableExcursion);
    }

    [Fact]
    public void Parse_UsThousandsSeparatorProfit_IsParsedCorrectly()
    {
        const string csv =
            "Instrument,Market pos.,Qty,Entry price,Exit price,Entry time,Exit time,Profit,Commission\n" +
            "ESH6,Long,10,5000.00,5010.00,2026-01-05 14:00:00,2026-01-05 14:05:00,\"1,000.00\",2.50\n";

        var parser = new NinjaTraderCsvParser();
        using var reader = new StringReader(csv);

        var result = parser.Parse(reader, CultureInfo.InvariantCulture);

        Assert.Empty(result.Errors);
        var row = Assert.Single(result.Rows);
        Assert.Equal(1002.50m, row.GrossPnL); // Profit (1,000.00) + Commission (2.50)
    }

    [Fact]
    public void Parse_MissingRequiredColumn_ReturnsError()
    {
        var parser = new NinjaTraderCsvParser();
        using var reader = new StringReader("Instrument,Qty\nESH6,1\n");

        var result = parser.Parse(reader);

        Assert.Empty(result.Rows);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void Parse_MalformedRow_IsSkippedAsErrorWithoutStoppingOtherRows()
    {
        var csv =
            "Instrument,Market pos.,Qty,Entry price,Exit price,Entry time,Exit time\n" +
            "ESH6,Long,not-a-number,5000,5010,2026-01-05 14:00:00,2026-01-05 14:05:00\n" +
            "MESH6,Long,1,5000,5010,2026-01-05 14:00:00,2026-01-05 14:05:00\n";

        var parser = new NinjaTraderCsvParser();
        using var reader = new StringReader(csv);

        var result = parser.Parse(reader, CultureInfo.InvariantCulture);

        Assert.Single(result.Errors);
        Assert.Single(result.Rows); // la segunda fila, válida, sí se importa
        Assert.Equal(2, result.Errors[0].LineNumber);
    }
}

public class TradovateCsvParserTests
{
    // Cabecera y formato reales del export "Reports → Performance" de Tradovate.
    private const string SampleCsv =
        "symbol,_priceFormat,_priceFormatType,_tickSize,buyFillId,sellFillId,qty,buyPrice,sellPrice,pnl,boughtTimestamp,soldTimestamp,duration\n" +
        "MESM6,-2,0,0.25,204569665,204584966,1,4174.25,4178.50,$21.25,01/05/2026 12:14:41,01/05/2026 13:31:26,1h 16min 45sec\n" +
        "MNQM6,-2,0,0.25,204590001,204589000,2,21010.00,21015.50,$(22.00),01/05/2026 15:20:10,01/05/2026 15:05:00,15min 10sec\n";

    [Fact]
    public void Parse_ValidCsv_InfersDirectionFromTimestampsAndParsesCurrencyPnl()
    {
        var parser = new TradovateCsvParser();
        using var reader = new StringReader(SampleCsv);

        var result = parser.Parse(reader);

        Assert.Empty(result.Errors);
        Assert.Equal(2, result.Rows.Count);

        // Fila 1: comprado antes de vendido → Long, pnl positivo con símbolo $.
        var longRow = result.Rows[0];
        Assert.Equal("MESM6", longRow.Symbol);
        Assert.Equal("Long", longRow.MarketPosition);
        Assert.Equal(1, longRow.Quantity);
        Assert.Equal(4174.25m, longRow.EntryPrice);
        Assert.Equal(4178.50m, longRow.ExitPrice);
        Assert.Equal(21.25m, longRow.GrossPnL);
        Assert.Equal(0m, longRow.Commission); // Tradovate no reporta comisión por trade
        Assert.Equal(new DateTime(2026, 1, 5, 12, 14, 41), longRow.EntryTime.DateTime);

        // Fila 2: vendido antes de comprado → Short, pnl negativo con paréntesis.
        var shortRow = result.Rows[1];
        Assert.Equal("Short", shortRow.MarketPosition);
        Assert.Equal(21015.50m, shortRow.EntryPrice); // entra vendiendo (sellPrice)
        Assert.Equal(21010.00m, shortRow.ExitPrice);  // sale recomprando (buyPrice)
        Assert.Equal(-22.00m, shortRow.GrossPnL);
        Assert.True(shortRow.EntryTime < shortRow.ExitTime);
    }

    [Fact]
    public void Parse_PnlWithThousandsSeparator_IsParsed()
    {
        const string csv =
            "symbol,qty,buyPrice,sellPrice,pnl,boughtTimestamp,soldTimestamp\n" +
            "ESM6,4,5000.00,5010.00,\"$2,000.00\",01/05/2026 12:00:00,01/05/2026 12:30:00\n";

        var parser = new TradovateCsvParser();
        using var reader = new StringReader(csv);

        var result = parser.Parse(reader);

        Assert.Empty(result.Errors);
        Assert.Equal(2000.00m, Assert.Single(result.Rows).GrossPnL);
    }

    [Fact]
    public void Parse_MissingRequiredColumn_ReturnsError()
    {
        var parser = new TradovateCsvParser();
        using var reader = new StringReader("symbol,qty\nESM6,1\n");

        var result = parser.Parse(reader);

        Assert.Empty(result.Rows);
        Assert.Single(result.Errors);
    }
}

public class CsvTradeImportServiceTests
{
    private const string NinjaTraderCsv =
        "Instrument,Market pos.,Qty,Entry price,Exit price,Entry time,Exit time,Profit,Commission\n" +
        "ESH6,Long,1,5000.00,5010.00,2026-01-05 14:00:00,2026-01-05 14:05:00,497.50,2.50\n";

    private const string TradovateCsv =
        "symbol,_priceFormat,_priceFormatType,_tickSize,buyFillId,sellFillId,qty,buyPrice,sellPrice,pnl,boughtTimestamp,soldTimestamp,duration\n" +
        "MESM6,-2,0,0.25,204569665,204584966,1,4174.25,4178.50,$21.25,01/05/2026 12:14:41,01/05/2026 13:31:26,1h 16min 45sec\n";

    private const string UserId = "user-1";

    private static async Task<(InMemoryDbContextFactory Factory, Guid AccountId)> SeedAccountAsync()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var propFirmId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        await using var db = await factory.CreateDbContextAsync();
        db.PropFirms.Add(new PropFirm { Id = propFirmId, Name = "Test Firm" });
        db.TradingAccounts.Add(new TradingAccount
        {
            Id = accountId,
            UserId = UserId,
            PropFirmId = propFirmId,
            DisplayName = "Acc",
            AccountSize = 50_000,
            PurchasedOn = new DateOnly(2026, 1, 1),
        });
        await db.SaveChangesAsync();

        return (factory, accountId);
    }

    private static CsvTradeImportService BuildService(InMemoryDbContextFactory factory) =>
        new(factory, new FakeCurrentUserAccessor(UserId));

    [Fact]
    public async Task ImportAsync_NinjaTraderCsv_CreatesTradeWithCsvImportSource()
    {
        var (factory, accountId) = await SeedAccountAsync();
        var sut = BuildService(factory);

        var summary = await sut.ImportAsync(accountId, new MemoryStream(Encoding.UTF8.GetBytes(NinjaTraderCsv)));

        Assert.Equal(1, summary.Imported);
        Assert.Equal(0, summary.Skipped);

        await using var db = await factory.CreateDbContextAsync();
        var trade = await db.Trades.Include(t => t.Executions).SingleAsync(t => t.AccountId == accountId);
        Assert.Equal(500m, trade.GrossPnL);
        Assert.Contains("NinjaTrader 8", trade.Notes);
        Assert.All(trade.Executions, e => Assert.Equal(TradeSourceType.CsvImport, e.Source));
    }

    [Fact]
    public async Task ImportAsync_TradovateCsv_IsAutodetectedAndImported()
    {
        var (factory, accountId) = await SeedAccountAsync();
        var sut = BuildService(factory);

        var summary = await sut.ImportAsync(accountId, new MemoryStream(Encoding.UTF8.GetBytes(TradovateCsv)));

        Assert.Equal(1, summary.Imported);
        Assert.Empty(summary.Errors);

        await using var db = await factory.CreateDbContextAsync();
        var trade = await db.Trades.Include(t => t.Executions).SingleAsync(t => t.AccountId == accountId);
        Assert.Equal("MESM6", trade.Symbol);
        Assert.Equal(TradeDirection.Long, trade.Direction);
        Assert.Equal(21.25m, trade.GrossPnL);
        Assert.Contains("Tradovate", trade.Notes);
        Assert.All(trade.Executions, e => Assert.Equal(TradeSourceType.CsvImport, e.Source));
    }

    [Fact]
    public async Task ImportAsync_UnknownFormat_ReturnsErrorWithoutImporting()
    {
        var (factory, accountId) = await SeedAccountAsync();
        var sut = BuildService(factory);

        var summary = await sut.ImportAsync(accountId, new MemoryStream(Encoding.UTF8.GetBytes("foo,bar\n1,2\n")));

        Assert.Equal(0, summary.Imported);
        Assert.Single(summary.Errors);

        await using var db = await factory.CreateDbContextAsync();
        Assert.Equal(0, await db.Trades.CountAsync(t => t.AccountId == accountId));
    }

    [Fact]
    public async Task ImportAsync_SameFileImportedTwice_SkipsAlreadyImportedRowsWithoutDuplicating()
    {
        var (factory, accountId) = await SeedAccountAsync();
        var sut = BuildService(factory);

        var first = await sut.ImportAsync(accountId, new MemoryStream(Encoding.UTF8.GetBytes(NinjaTraderCsv)));
        var second = await sut.ImportAsync(accountId, new MemoryStream(Encoding.UTF8.GetBytes(NinjaTraderCsv)));

        Assert.Equal(1, first.Imported);
        Assert.Equal(0, second.Imported);
        Assert.Equal(1, second.Skipped);

        await using var db = await factory.CreateDbContextAsync();
        Assert.Equal(1, await db.Trades.CountAsync(t => t.AccountId == accountId));
    }
}
