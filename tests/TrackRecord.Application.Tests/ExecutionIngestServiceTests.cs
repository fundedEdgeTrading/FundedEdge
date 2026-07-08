using Microsoft.EntityFrameworkCore;
using TrackRecord.Application.Dtos;
using TrackRecord.Domain.Entities;
using TrackRecord.Domain.Enums;
using TrackRecord.Infrastructure.Services;

namespace TrackRecord.Application.Tests;

public class ExecutionIngestServiceTests
{
    private const string UserId = "user-1";

    private static async Task<(InMemoryDbContextFactory Factory, Guid AccountId)> SeedAccountAsync(string externalAccountId = "NT8-DEMO-1")
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
            ExternalAccountId = externalAccountId,
            AccountSize = 50_000,
            PurchasedOn = new DateOnly(2026, 1, 1),
        });
        db.Instruments.Add(new Instrument { Id = Guid.NewGuid(), Root = "ES", Name = "E-mini S&P 500", TickSize = 0.25m, TickValue = 12.50m });
        await db.SaveChangesAsync();

        return (factory, accountId);
    }

    private static IngestExecutionRequest Fill(string externalId, OrderSide side, decimal price, DateTimeOffset at, string accountExternalId = "NT8-DEMO-1") =>
        new(externalId, TradeSourceType.NinjaTraderAddOn, accountExternalId, "ESH6", side, 1, price, at, 0m);

    [Fact]
    public async Task IngestAsync_SameExternalIdTwice_OnlyInsertsOneExecution()
    {
        var (factory, _) = await SeedAccountAsync();
        var sut = new ExecutionIngestService(factory, new TradeRebuildService(factory));

        var first = await sut.IngestAsync(Fill("nt8-1", OrderSide.Buy, 5000m, new DateTimeOffset(2026, 1, 5, 14, 0, 0, TimeSpan.Zero)), UserId);
        var second = await sut.IngestAsync(Fill("nt8-1", OrderSide.Buy, 5000m, new DateTimeOffset(2026, 1, 5, 14, 0, 0, TimeSpan.Zero)), UserId);

        Assert.True(first.Inserted);
        Assert.False(second.Inserted);

        await using var db = await factory.CreateDbContextAsync();
        var count = await db.Executions.CountAsync(e => e.ExternalId == "nt8-1");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task IngestAsync_UnknownAccountExternalId_DoesNotInsertAndReportsUnresolved()
    {
        var (factory, _) = await SeedAccountAsync();
        var sut = new ExecutionIngestService(factory, new TradeRebuildService(factory));

        var result = await sut.IngestAsync(Fill("nt8-1", OrderSide.Buy, 5000m, DateTimeOffset.UtcNow, accountExternalId: "NO-SUCH-ACCOUNT"), UserId);

        Assert.False(result.AccountResolved);
        Assert.False(result.Inserted);

        await using var db = await factory.CreateDbContextAsync();
        Assert.Equal(0, await db.Executions.CountAsync());
    }

    [Fact]
    public async Task IngestAsync_EntryThenExitFill_RebuildsAClosedTradeAutomatically()
    {
        var (factory, accountId) = await SeedAccountAsync();
        var sut = new ExecutionIngestService(factory, new TradeRebuildService(factory));

        await sut.IngestAsync(Fill("nt8-entry", OrderSide.Buy, 5000m, new DateTimeOffset(2026, 1, 5, 14, 0, 0, TimeSpan.Zero)), UserId);
        var exitResult = await sut.IngestAsync(Fill("nt8-exit", OrderSide.Sell, 5010m, new DateTimeOffset(2026, 1, 5, 14, 5, 0, TimeSpan.Zero)), UserId);

        Assert.Equal(1, exitResult.TradesRebuilt);

        await using var db = await factory.CreateDbContextAsync();
        var trade = await db.Trades.Include(t => t.Executions).SingleAsync(t => t.AccountId == accountId);
        Assert.Equal(500m, trade.GrossPnL); // 10 puntos * 50 $/punto
        Assert.Equal(TradeSourceType.NinjaTraderAddOn, trade.Executions.First().Source);
    }

    [Fact]
    public async Task IngestAsync_ReIngestingAfterMoreFillsArrive_RebuildsConsistentTradeState()
    {
        // Simula un backfill: llega primero solo la entrada + una salida parcial, luego el resto.
        var (factory, accountId) = await SeedAccountAsync();
        var sut = new ExecutionIngestService(factory, new TradeRebuildService(factory));

        await sut.IngestAsync(Fill("nt8-1", OrderSide.Buy, 5000m, new DateTimeOffset(2026, 1, 5, 14, 0, 0, TimeSpan.Zero)), UserId);
        await sut.IngestAsync(new IngestExecutionRequest("nt8-2", TradeSourceType.NinjaTraderAddOn, "NT8-DEMO-1", "ESH6", OrderSide.Buy, 1, 5010m, new DateTimeOffset(2026, 1, 5, 14, 1, 0, TimeSpan.Zero), 0m), UserId);
        await sut.IngestAsync(new IngestExecutionRequest("nt8-3", TradeSourceType.NinjaTraderAddOn, "NT8-DEMO-1", "ESH6", OrderSide.Sell, 2, 5020m, new DateTimeOffset(2026, 1, 5, 14, 5, 0, TimeSpan.Zero), 0m), UserId);

        await using var db = await factory.CreateDbContextAsync();
        var trade = await db.Trades.SingleAsync(t => t.AccountId == accountId);
        Assert.Equal(2, trade.Quantity);
        Assert.Equal(5005m, trade.AvgEntryPrice); // (5000+5010)/2
        Assert.Equal(5020m, trade.AvgExitPrice);
        Assert.Equal(1500m, trade.GrossPnL); // (5020-5005)*2*50
    }
}
