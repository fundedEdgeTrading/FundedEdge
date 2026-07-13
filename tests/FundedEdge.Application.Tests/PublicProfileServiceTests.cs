using FundedEdge.Domain.Entities;
using FundedEdge.Domain.Enums;
using FundedEdge.Infrastructure.Identity;
using FundedEdge.Infrastructure.Services;

namespace FundedEdge.Application.Tests;

public class PublicProfileServiceTests
{
    private const string UserId = "user-1";

    private static async Task<InMemoryDbContextFactory> SeedUserAsync(PlanTier tier)
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        await using var db = await factory.CreateDbContextAsync();
        db.Users.Add(new ApplicationUser { Id = UserId, UserName = "user@test.com", DisplayName = "Trader Uno", PlanTier = tier });
        await db.SaveChangesAsync();
        return factory;
    }

    private static PublicProfileService BuildService(InMemoryDbContextFactory factory) =>
        new(factory, new FakeCurrentUserAccessor(UserId), new PlanService(factory, new FakeCurrentUserAccessor(UserId)));

    [Fact]
    public async Task Starter_CannotEnablePublicProfile()
    {
        var factory = await SeedUserAsync(PlanTier.Starter);
        var service = BuildService(factory);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.EnableAsync());
    }

    [Fact]
    public async Task Starter_GetOwnSettings_ReportsCannotPublish()
    {
        var factory = await SeedUserAsync(PlanTier.Starter);
        var settings = await BuildService(factory).GetOwnSettingsAsync();

        Assert.False(settings.CanPublish);
        Assert.False(settings.IsEnabled);
        Assert.Null(settings.Slug);
    }

    [Fact]
    public async Task Elite_Enable_GeneratesSlugAndIsRetrievablePublicly()
    {
        var factory = await SeedUserAsync(PlanTier.Elite);
        var service = BuildService(factory);

        var settings = await service.EnableAsync();

        Assert.True(settings.IsEnabled);
        Assert.False(string.IsNullOrWhiteSpace(settings.Slug));

        var view = await service.GetPublicViewAsync(settings.Slug!);
        Assert.NotNull(view);
        Assert.Equal("Trader Uno", view!.DisplayName);
    }

    [Fact]
    public async Task Disable_MakesPublicViewUnavailable()
    {
        var factory = await SeedUserAsync(PlanTier.Elite);
        var service = BuildService(factory);
        var settings = await service.EnableAsync();

        await service.DisableAsync();

        Assert.Null(await service.GetPublicViewAsync(settings.Slug!));
    }

    [Fact]
    public async Task UnknownSlug_ReturnsNull()
    {
        var factory = await SeedUserAsync(PlanTier.Elite);
        Assert.Null(await BuildService(factory).GetPublicViewAsync("does-not-exist"));
    }

    [Fact]
    public async Task PublicView_AggregatesFundedAccountsPassRateAndTradingStats()
    {
        var factory = await SeedUserAsync(PlanTier.Elite);
        var propFirmId = Guid.NewGuid();
        Guid accountId;

        await using (var db = await factory.CreateDbContextAsync())
        {
            db.PropFirms.Add(new PropFirm { Id = propFirmId, Name = "Test Firm" });
            accountId = Guid.NewGuid();
            db.TradingAccounts.Add(new TradingAccount
            {
                Id = accountId,
                UserId = UserId,
                PropFirmId = propFirmId,
                DisplayName = "Acc",
                AccountSize = 50_000,
                PurchasedOn = new DateOnly(2026, 1, 1),
                Stage = AccountStage.Funded,
                FundedOn = new DateOnly(2026, 2, 1),
            });
            db.TradingAccounts.Add(new TradingAccount
            {
                UserId = UserId,
                PropFirmId = propFirmId,
                DisplayName = "Acc failed",
                AccountSize = 50_000,
                PurchasedOn = new DateOnly(2026, 1, 1),
                Stage = AccountStage.Failed,
            });
            db.Trades.AddRange(
                new Trade { AccountId = accountId, Symbol = "ES", Direction = TradeDirection.Long, Quantity = 1, ClosedAt = DateTimeOffset.UtcNow, GrossPnL = 500m, Commissions = 0m, RiskedAmount = 100m },
                new Trade { AccountId = accountId, Symbol = "ES", Direction = TradeDirection.Long, Quantity = 1, ClosedAt = DateTimeOffset.UtcNow, GrossPnL = -200m, Commissions = 0m, RiskedAmount = 100m });
            await db.SaveChangesAsync();
        }

        var service = BuildService(factory);
        var settings = await service.EnableAsync();
        var view = await service.GetPublicViewAsync(settings.Slug!);

        Assert.NotNull(view);
        Assert.Equal(1, view!.AccountsFunded);
        Assert.Equal(0.5, view.PassRate);
        Assert.Equal(2, view.TotalTrades);
        Assert.Equal(0.5, view.WinRate);
        Assert.Equal(2.5, view.ProfitFactor);
    }
}
