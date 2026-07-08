using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using TrackRecord.Domain.Entities;
using TrackRecord.Domain.Enums;
using TrackRecord.Infrastructure.Integrations.NinjaTrader;

namespace TrackRecord.Application.Tests;

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

public class CsvTradeImportServiceTests
{
    private const string SampleCsv =
        "Instrument,Market pos.,Qty,Entry price,Exit price,Entry time,Exit time,Profit,Commission\n" +
        "ESH6,Long,1,5000.00,5010.00,2026-01-05 14:00:00,2026-01-05 14:05:00,497.50,2.50\n";

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
    public async Task ImportAsync_ValidCsv_CreatesTradeWithCsvImportSource()
    {
        var (factory, accountId) = await SeedAccountAsync();
        var sut = BuildService(factory);

        var summary = await sut.ImportAsync(accountId, new MemoryStream(Encoding.UTF8.GetBytes(SampleCsv)));

        Assert.Equal(1, summary.Imported);
        Assert.Equal(0, summary.Skipped);

        await using var db = await factory.CreateDbContextAsync();
        var trade = await db.Trades.Include(t => t.Executions).SingleAsync(t => t.AccountId == accountId);
        Assert.Equal(500m, trade.GrossPnL);
        Assert.All(trade.Executions, e => Assert.Equal(TradeSourceType.CsvImport, e.Source));
    }

    [Fact]
    public async Task ImportAsync_SameFileImportedTwice_SkipsAlreadyImportedRowsWithoutDuplicating()
    {
        var (factory, accountId) = await SeedAccountAsync();
        var sut = BuildService(factory);

        var first = await sut.ImportAsync(accountId, new MemoryStream(Encoding.UTF8.GetBytes(SampleCsv)));
        var second = await sut.ImportAsync(accountId, new MemoryStream(Encoding.UTF8.GetBytes(SampleCsv)));

        Assert.Equal(1, first.Imported);
        Assert.Equal(0, second.Imported);
        Assert.Equal(1, second.Skipped);

        await using var db = await factory.CreateDbContextAsync();
        Assert.Equal(1, await db.Trades.CountAsync(t => t.AccountId == accountId));
    }
}
