using Microsoft.AspNetCore.DataProtection;
using TrackRecord.Domain.Enums;
using TrackRecord.Infrastructure.Services;
using TrackRecord.Infrastructure.Settings;

namespace TrackRecord.Application.Tests;

public class CurrencyPreferenceServiceTests
{
    private const string UserId = "user-1";

    private static DataProtectedIntegrationSettingsStore BuildStore(InMemoryDbContextFactory factory) =>
        new(factory, new EphemeralDataProtectionProvider());

    [Fact]
    public async Task GetAsync_NothingStored_DefaultsToUsd()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var service = new CurrencyPreferenceService(BuildStore(factory), new FakeCurrentUserAccessor(UserId));

        Assert.Equal(Currency.Usd, await service.GetAsync());
    }

    [Fact]
    public async Task SetThenGet_RoundTrips()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var service = new CurrencyPreferenceService(BuildStore(factory), new FakeCurrentUserAccessor(UserId));

        await service.SetAsync(Currency.Eur);

        Assert.Equal(Currency.Eur, await service.GetAsync());
    }

    [Fact]
    public async Task GetAsync_CorruptedValue_FallsBackToUsd()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var store = BuildStore(factory);
        await store.SetAsync($"{UserId}:Ui:Currency", "NotACurrency");
        var service = new CurrencyPreferenceService(store, new FakeCurrentUserAccessor(UserId));

        Assert.Equal(Currency.Usd, await service.GetAsync());
    }

    [Fact]
    public async Task GetAsync_Unauthenticated_DefaultsToUsdWithoutThrowing()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var service = new CurrencyPreferenceService(BuildStore(factory), new FakeCurrentUserAccessor(null));

        Assert.Equal(Currency.Usd, await service.GetAsync());
    }
}
