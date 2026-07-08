using Microsoft.EntityFrameworkCore;
using TrackRecord.Application.Dtos;
using TrackRecord.Domain.Entities;
using TrackRecord.Domain.Enums;
using TrackRecord.Infrastructure.Services;

namespace TrackRecord.Application.Tests;

public class TradingAccountServiceTests
{
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

    private static TradingAccountService BuildService(InMemoryDbContextFactory factory) =>
        new(factory, new FakeCurrentUserAccessor(UserId), new PlanService(factory, new FakeCurrentUserAccessor(UserId)));

    [Fact]
    public async Task AddTradeAsync_CreatesTradeWithTwoManualExecutions()
    {
        var (factory, accountId) = await SeedAccountAsync();
        var sut = BuildService(factory);

        var tradeId = await sut.AddTradeAsync(new CreateTradeRequest(
            accountId, "mes", TradeDirection.Long, 2,
            5000m, 5010m,
            new DateTimeOffset(2026, 1, 5, 14, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 5, 14, 30, 0, TimeSpan.Zero),
            250m, 4.5m, 100m, "setup-A", "nota"));

        await using var db = await factory.CreateDbContextAsync();
        var trade = await db.Trades.SingleAsync(t => t.Id == tradeId);
        var executions = await db.Executions.Where(e => e.TradeId == tradeId).OrderBy(e => e.ExecutedAt).ToListAsync();

        Assert.Equal("MES", trade.Symbol);
        Assert.Equal(2, executions.Count);
        Assert.All(executions, e => Assert.Equal(TradeSourceType.Manual, e.Source));

        var entry = executions[0];
        Assert.Equal(OrderSide.Buy, entry.Side);           // Long -> entra comprando
        Assert.Equal(5000m, entry.Price);
        Assert.Equal(0m, entry.Commission);

        var exit = executions[1];
        Assert.Equal(OrderSide.Sell, exit.Side);           // Long -> sale vendiendo
        Assert.Equal(5010m, exit.Price);
        Assert.Equal(4.5m, exit.Commission);                // comisión total imputada a la salida

        // (Source, ExternalId) debe ser único por trade, con prefijo identificable por origen.
        Assert.StartsWith("manual-", entry.ExternalId);
        Assert.EndsWith("-entry", entry.ExternalId);
        Assert.StartsWith("manual-", exit.ExternalId);
        Assert.EndsWith("-exit", exit.ExternalId);
        Assert.NotEqual(entry.ExternalId, exit.ExternalId);
    }

    [Fact]
    public async Task DeleteTradeAsync_RemovesTradeAndItsManualExecutions_ButKeepsNonManualOrphaned()
    {
        var (factory, accountId) = await SeedAccountAsync();
        var sut = BuildService(factory);

        var tradeId = await sut.AddTradeAsync(new CreateTradeRequest(
            accountId, "ES", TradeDirection.Short, 1,
            5000m, 4990m,
            new DateTimeOffset(2026, 1, 5, 14, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 5, 14, 5, 0, TimeSpan.Zero),
            50m, 2m, null, null, null));

        // Simula un fill real (Tradovate) ya vinculado al mismo trade, como haría el futuro sync.
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Executions.Add(new Execution
            {
                AccountId = accountId,
                TradeId = tradeId,
                ExternalId = "tv-12345",
                Source = TradeSourceType.Tradovate,
                Symbol = "ES",
                Side = OrderSide.Sell,
                Quantity = 1,
                Price = 5000m,
                ExecutedAt = new DateTimeOffset(2026, 1, 5, 14, 0, 0, TimeSpan.Zero),
            });
            await db.SaveChangesAsync();
        }

        await sut.DeleteTradeAsync(tradeId);

        await using var verifyDb = await factory.CreateDbContextAsync();
        Assert.False(await verifyDb.Trades.AnyAsync(t => t.Id == tradeId));
        Assert.False(await verifyDb.Executions.AnyAsync(e => e.Source == TradeSourceType.Manual));

        var orphaned = await verifyDb.Executions.SingleAsync(e => e.Source == TradeSourceType.Tradovate);
        Assert.Null(orphaned.TradeId);
    }

    [Fact]
    public async Task DeleteAsync_RemovesAccountAndAllAssociatedData()
    {
        var (factory, accountId) = await SeedAccountAsync();
        var sut = BuildService(factory);

        var tradeId = await sut.AddTradeAsync(new CreateTradeRequest(
            accountId, "MES", TradeDirection.Long, 1,
            5000m, 5010m,
            new DateTimeOffset(2026, 1, 5, 14, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 5, 14, 30, 0, TimeSpan.Zero),
            50m, 2m, null, null, null));
        await sut.AddCostAsync(new AddAccountCostRequest(accountId, CostKind.Evaluation, 199m, new DateOnly(2026, 1, 1), null));
        await sut.AddPayoutAsync(new AddPayoutRequest(accountId, 500m, 500m, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 3), PayoutStatus.Paid, null));

        // Fill no manual (p.ej. Tradovate) y evento de ciclo de vida también vinculados a la
        // cuenta, como los que provocaban el conflicto de FK original al confiar en el borrado en
        // cascada de la base de datos.
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Executions.Add(new Execution
            {
                AccountId = accountId,
                TradeId = tradeId,
                ExternalId = "tv-99",
                Source = TradeSourceType.Tradovate,
                Symbol = "MES",
                Side = OrderSide.Buy,
                Quantity = 1,
                Price = 5000m,
                ExecutedAt = new DateTimeOffset(2026, 1, 5, 14, 0, 0, TimeSpan.Zero),
            });
            db.AccountEvents.Add(new AccountEvent
            {
                AccountId = accountId,
                FromStage = AccountStage.Evaluation,
                ToStage = AccountStage.Funded,
                OccurredAt = new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero),
            });
            await db.SaveChangesAsync();
        }

        await sut.DeleteAsync(accountId);

        await using var verifyDb = await factory.CreateDbContextAsync();
        Assert.False(await verifyDb.TradingAccounts.AnyAsync(a => a.Id == accountId));
        Assert.False(await verifyDb.Trades.AnyAsync(t => t.AccountId == accountId));
        Assert.False(await verifyDb.Executions.AnyAsync(e => e.AccountId == accountId));
        Assert.False(await verifyDb.AccountCosts.AnyAsync(c => c.AccountId == accountId));
        Assert.False(await verifyDb.Payouts.AnyAsync(p => p.AccountId == accountId));
        Assert.False(await verifyDb.AccountEvents.AnyAsync(e => e.AccountId == accountId));
    }

    [Fact]
    public async Task DeleteAsync_UnknownAccount_DoesNotThrow()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var sut = BuildService(factory);

        await sut.DeleteAsync(Guid.NewGuid());
    }

    [Fact]
    public async Task UpdateConnectionAsync_SetsFeedAndExternalAccountId()
    {
        var (factory, accountId) = await SeedAccountAsync();
        var sut = BuildService(factory);

        await sut.UpdateConnectionAsync(new UpdateAccountConnectionRequest(accountId, DataFeedType.Tradovate, "12345"));

        await using var db = await factory.CreateDbContextAsync();
        var account = await db.TradingAccounts.SingleAsync(a => a.Id == accountId);
        Assert.Equal(DataFeedType.Tradovate, account.Feed);
        Assert.Equal("12345", account.ExternalAccountId);
    }

    [Fact]
    public async Task UpdateConnectionAsync_BlankExternalAccountId_IsStoredAsNull()
    {
        var (factory, accountId) = await SeedAccountAsync();
        var sut = BuildService(factory);

        await sut.UpdateConnectionAsync(new UpdateAccountConnectionRequest(accountId, DataFeedType.Manual, "   "));

        await using var db = await factory.CreateDbContextAsync();
        var account = await db.TradingAccounts.SingleAsync(a => a.Id == accountId);
        Assert.Null(account.ExternalAccountId);
    }

    [Fact]
    public async Task UpdateConnectionAsync_UnknownAccount_ThrowsKeyNotFound()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var sut = BuildService(factory);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.UpdateConnectionAsync(new UpdateAccountConnectionRequest(Guid.NewGuid(), DataFeedType.Manual, null)));
    }

    [Fact]
    public async Task RenameAsync_UpdatesDisplayNameAndTrimsWhitespace()
    {
        var (factory, accountId) = await SeedAccountAsync();
        var sut = BuildService(factory);

        await sut.RenameAsync(new RenameAccountRequest(accountId, "  Cuenta Fondeo Apex  "));

        await using var db = await factory.CreateDbContextAsync();
        var account = await db.TradingAccounts.SingleAsync(a => a.Id == accountId);
        Assert.Equal("Cuenta Fondeo Apex", account.DisplayName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RenameAsync_BlankName_ThrowsArgument(string name)
    {
        var (factory, accountId) = await SeedAccountAsync();
        var sut = BuildService(factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.RenameAsync(new RenameAccountRequest(accountId, name)));
    }

    [Fact]
    public async Task RenameAsync_UnknownAccount_ThrowsKeyNotFound()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var sut = BuildService(factory);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.RenameAsync(new RenameAccountRequest(Guid.NewGuid(), "Nuevo nombre")));
    }

    private static async Task<(InMemoryDbContextFactory Factory, Guid AccountId)> SeedFundedAccountAsync(
        int? minDaysBetweenPayouts, DateOnly fundedOn, DateOnly? lastPayoutRequestedOn = null)
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var propFirmId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        await using var db = await factory.CreateDbContextAsync();
        db.PropFirms.Add(new PropFirm { Id = propFirmId, Name = "Test Firm", MinDaysBetweenPayouts = minDaysBetweenPayouts });
        var account = new TradingAccount
        {
            Id = accountId,
            UserId = UserId,
            PropFirmId = propFirmId,
            DisplayName = "Acc",
            AccountSize = 50_000,
            PurchasedOn = new DateOnly(2026, 1, 1),
            Stage = AccountStage.Funded,
            FundedOn = fundedOn,
        };
        if (lastPayoutRequestedOn is not null)
        {
            account.Payouts.Add(new Payout
            {
                AmountRequested = 1000m,
                AmountReceived = 1000m,
                RequestedOn = lastPayoutRequestedOn.Value,
                Status = PayoutStatus.Paid,
            });
        }
        db.TradingAccounts.Add(account);
        await db.SaveChangesAsync();

        return (factory, accountId);
    }

    [Fact]
    public async Task GetByIdAsync_FirmWithoutPayoutRule_NextPayoutEligibleOnIsNull()
    {
        var (factory, accountId) = await SeedFundedAccountAsync(minDaysBetweenPayouts: null, fundedOn: new DateOnly(2026, 1, 1));
        var sut = BuildService(factory);

        var result = await sut.GetByIdAsync(accountId);

        Assert.Null(result!.NextPayoutEligibleOn);
    }

    [Fact]
    public async Task GetByIdAsync_NoPriorPayout_CountsFromFundedOn()
    {
        var (factory, accountId) = await SeedFundedAccountAsync(minDaysBetweenPayouts: 14, fundedOn: new DateOnly(2026, 1, 1));
        var sut = BuildService(factory);

        var result = await sut.GetByIdAsync(accountId);

        Assert.Equal(new DateOnly(2026, 1, 15), result!.NextPayoutEligibleOn);
    }

    [Fact]
    public async Task GetByIdAsync_WithPriorPayout_CountsFromLastPayoutRequestedOn()
    {
        var (factory, accountId) = await SeedFundedAccountAsync(
            minDaysBetweenPayouts: 14, fundedOn: new DateOnly(2026, 1, 1), lastPayoutRequestedOn: new DateOnly(2026, 3, 1));
        var sut = BuildService(factory);

        var result = await sut.GetByIdAsync(accountId);

        Assert.Equal(new DateOnly(2026, 3, 15), result!.NextPayoutEligibleOn);
    }

    [Fact]
    public async Task GetByIdAsync_AccountNotFunded_NextPayoutEligibleOnIsNull()
    {
        var (factory, accountId) = await SeedAccountAsync(); // Stage por defecto: Evaluation
        await using (var db = await factory.CreateDbContextAsync())
        {
            var firm = await db.PropFirms.SingleAsync();
            firm.MinDaysBetweenPayouts = 14;
            await db.SaveChangesAsync();
        }
        var sut = BuildService(factory);

        var result = await sut.GetByIdAsync(accountId);

        Assert.Null(result!.NextPayoutEligibleOn);
    }
}
