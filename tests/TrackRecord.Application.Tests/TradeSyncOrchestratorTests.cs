using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TrackRecord.Domain.Entities;
using TrackRecord.Domain.Enums;
using TrackRecord.Infrastructure.Identity;
using TrackRecord.Infrastructure.Integrations.Tradovate;
using TrackRecord.Infrastructure.Services;

namespace TrackRecord.Application.Tests;

public class TradeSyncOrchestratorTests
{
    private const string UserId = "user-1";

    private sealed class FakeTradovateClient : ITradovateClient
    {
        public List<(long AccountId, DateTimeOffset Since)> Calls { get; } = [];
        public IReadOnlyList<TradovateFill> FillsToReturn { get; set; } = [];

        public Task<IReadOnlyList<TradovateAccount>> GetAccountsAsync(string userId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<TradovateAccount>>([]);

        public Task<IReadOnlyList<TradovateFill>> GetFillsAsync(string userId, long accountId, DateTimeOffset since, CancellationToken ct = default)
        {
            Calls.Add((accountId, since));
            return Task.FromResult(FillsToReturn);
        }
    }

    private sealed class FakeCredentialStore(TradovateCredentials? credentials) : ITradovateCredentialStore
    {
        public Task<TradovateCredentials?> GetCredentialsAsync(string userId, CancellationToken ct = default) => Task.FromResult(credentials);
    }

    private static readonly TradovateCredentials Credentials = new("user", "pass", 1, "sec", "device");

    private static async Task<(InMemoryDbContextFactory Factory, Guid AccountId)> SeedAccountAsync(
        DataFeedType feed = DataFeedType.Tradovate,
        AccountStage stage = AccountStage.Evaluation,
        string externalAccountId = "12345")
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
            Feed = feed,
            Stage = stage,
            AccountSize = 50_000,
            PurchasedOn = new DateOnly(2026, 1, 1),
        });
        db.Instruments.Add(new Instrument { Id = Guid.NewGuid(), Root = "ES", Name = "E-mini S&P 500", TickSize = 0.25m, TickValue = 12.50m });
        await db.SaveChangesAsync();

        return (factory, accountId);
    }

    private static TradeSyncOrchestrator BuildOrchestrator(InMemoryDbContextFactory factory, FakeTradovateClient tradovateClient, bool credentialsConfigured = true)
    {
        var rebuildService = new TradeRebuildService(factory);
        var ingestService = new ExecutionIngestService(factory, rebuildService);
        return new TradeSyncOrchestrator(
            factory, tradovateClient, new FakeCredentialStore(credentialsConfigured ? Credentials : null), ingestService,
            new FakeCurrentUserAccessor(UserId), new PlanService(factory, new FakeCurrentUserAccessor(UserId)),
            NullLogger<TradeSyncOrchestrator>.Instance);
    }

    [Fact]
    public async Task SyncAllAccountsAsync_NoCredentialsConfigured_DoesNothing()
    {
        var (factory, _) = await SeedAccountAsync();
        var client = new FakeTradovateClient();
        var sut = BuildOrchestrator(factory, client, credentialsConfigured: false);

        var ingested = await sut.SyncAllAccountsAsync();

        Assert.Equal(0, ingested);
        Assert.Empty(client.Calls);
    }

    [Fact]
    public async Task SyncAllAccountsAsync_StarterPlan_DoesNotCallTradovate()
    {
        var (factory, _) = await SeedAccountAsync();
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Users.Add(new ApplicationUser { Id = UserId, UserName = "user@test.com", PlanTier = PlanTier.Starter });
            await db.SaveChangesAsync();
        }
        var client = new FakeTradovateClient();
        var sut = BuildOrchestrator(factory, client); // credenciales configuradas: el gate que corta aquí es el del plan, no el de credenciales.

        var ingested = await sut.SyncAllAccountsAsync();

        Assert.Equal(0, ingested);
        Assert.Empty(client.Calls);
    }

    [Fact]
    public async Task SyncAccountAsync_NonTradovateFeed_SkipsAccount()
    {
        var (factory, accountId) = await SeedAccountAsync(feed: DataFeedType.NinjaTrader);
        var client = new FakeTradovateClient();
        var sut = BuildOrchestrator(factory, client);

        var ingested = await sut.SyncAccountAsync(accountId);

        Assert.Equal(0, ingested);
        Assert.Empty(client.Calls);
    }

    [Theory]
    [InlineData(AccountStage.Failed)]
    [InlineData(AccountStage.Withdrawn)]
    [InlineData(AccountStage.Expired)]
    public async Task SyncAllAccountsAsync_TerminalStage_IsExcludedFromSync(AccountStage terminalStage)
    {
        var (factory, _) = await SeedAccountAsync(stage: terminalStage);
        var client = new FakeTradovateClient();
        var sut = BuildOrchestrator(factory, client);

        await sut.SyncAllAccountsAsync();

        Assert.Empty(client.Calls);
    }

    [Fact]
    public async Task SyncAccountAsync_FirstSync_UsesPurchasedOnAsWatermark()
    {
        var (factory, accountId) = await SeedAccountAsync();
        var client = new FakeTradovateClient();
        var sut = BuildOrchestrator(factory, client);

        await sut.SyncAccountAsync(accountId);

        var call = Assert.Single(client.Calls);
        Assert.Equal(12345L, call.AccountId);
        Assert.Equal(new DateOnly(2026, 1, 1).ToDateTime(TimeOnly.MinValue), call.Since.DateTime);
    }

    [Fact]
    public async Task SyncAccountAsync_IngestsFillsAndBuildsTradeThroughTheSharedPipeline()
    {
        var (factory, accountId) = await SeedAccountAsync();
        var client = new FakeTradovateClient
        {
            FillsToReturn =
            [
                new TradovateFill(1, "ESH6", "Buy", 5000m, 1, new DateTimeOffset(2026, 1, 5, 14, 0, 0, TimeSpan.Zero)),
                new TradovateFill(2, "ESH6", "Sell", 5010m, 1, new DateTimeOffset(2026, 1, 5, 14, 5, 0, TimeSpan.Zero)),
            ],
        };
        var sut = BuildOrchestrator(factory, client);

        var ingested = await sut.SyncAccountAsync(accountId);

        Assert.Equal(2, ingested);

        await using var db = await factory.CreateDbContextAsync();
        var trade = await db.Trades.SingleAsync(t => t.AccountId == accountId);
        Assert.Equal(500m, trade.GrossPnL);

        var executions = await db.Executions.Where(e => e.AccountId == accountId).ToListAsync();
        Assert.All(executions, e => Assert.Equal(TradeSourceType.Tradovate, e.Source));
    }

    [Fact]
    public async Task SyncAccountAsync_CalledAgain_UsesLastExecutionMinusOverlapAsWatermark()
    {
        var (factory, accountId) = await SeedAccountAsync();
        var client = new FakeTradovateClient
        {
            FillsToReturn = [new TradovateFill(1, "ESH6", "Buy", 5000m, 1, new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero))],
        };
        var sut = BuildOrchestrator(factory, client);

        await sut.SyncAccountAsync(accountId); // primera sync: siembra un fill

        client.FillsToReturn = []; // segunda sync: sin fills nuevos, solo interesa el watermark usado
        await sut.SyncAccountAsync(accountId);

        Assert.Equal(2, client.Calls.Count);
        var secondCallSince = client.Calls[1].Since;
        Assert.Equal(new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero), secondCallSince); // 10:00 - 1h de solape
    }

    [Fact]
    public async Task SyncAccountAsync_NonNumericExternalAccountId_SkipsGracefully()
    {
        var (factory, accountId) = await SeedAccountAsync(externalAccountId: "Sim101"); // id de NinjaTrader, no numérico
        var client = new FakeTradovateClient();
        var sut = BuildOrchestrator(factory, client);

        var ingested = await sut.SyncAccountAsync(accountId);

        Assert.Equal(0, ingested);
        Assert.Empty(client.Calls);
    }
}
