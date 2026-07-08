using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using TrackRecord.Infrastructure.Ai;
using TrackRecord.Infrastructure.Integrations.Tradovate;
using TrackRecord.Infrastructure.Services;
using TrackRecord.Infrastructure.Settings;

namespace TrackRecord.Application.Tests;

public class IntegrationSettingsServiceTests
{
    private const string UserId = "user-1";

    private static DataProtectedIntegrationSettingsStore BuildStore(InMemoryDbContextFactory factory) =>
        new(factory, new EphemeralDataProtectionProvider());

    private static IntegrationSettingsService BuildService(
        InMemoryDbContextFactory factory, IIntegrationSettingsStore store, IConfiguration? configuration = null, bool aiConfigured = false) =>
        new(store, new ConfigurationTradovateCredentialStore(configuration ?? new ConfigurationBuilder().Build()),
            new FakeCurrentUserAccessor(UserId), new AiOptions(aiConfigured));

    [Fact]
    public async Task Store_SetThenGet_RoundTripsThroughEncryption()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var store = BuildStore(factory);

        await store.SetAsync("Tradovate:Name", "trader1");
        var value = await store.GetAsync("Tradovate:Name");

        Assert.Equal("trader1", value);
    }

    [Fact]
    public async Task Store_SetWithNullValue_DeletesKey()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var store = BuildStore(factory);

        await store.SetAsync("Tradovate:Name", "trader1");
        await store.SetAsync("Tradovate:Name", null);

        Assert.Null(await store.GetAsync("Tradovate:Name"));
    }

    [Fact]
    public async Task Store_FindKeyPrefixByValueAsync_FindsMatchingUserAmongSeveral()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var store = BuildStore(factory);

        await store.SetAsync("user-a:Ingest:NinjaTraderApiKey", "key-a");
        await store.SetAsync("user-b:Ingest:NinjaTraderApiKey", "key-b");

        var matched = await store.FindKeyPrefixByValueAsync(":Ingest:NinjaTraderApiKey", "key-b");

        Assert.Equal("user-b", matched);
    }

    [Fact]
    public async Task Store_FindKeyPrefixByValueAsync_NoMatch_ReturnsNull()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var store = BuildStore(factory);
        await store.SetAsync("user-a:Ingest:NinjaTraderApiKey", "key-a");

        Assert.Null(await store.FindKeyPrefixByValueAsync(":Ingest:NinjaTraderApiKey", "wrong-key"));
    }

    [Fact]
    public async Task GetTradovateSettingsAsync_NothingConfigured_ReturnsNotConfigured()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var service = BuildService(factory, BuildStore(factory));

        var result = await service.GetTradovateSettingsAsync();

        Assert.False(result.IsConfigured);
    }

    [Fact]
    public async Task SaveTradovateSettingsAsync_ThenGet_PrefersDatabaseOverConfiguration()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var store = BuildStore(factory);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tradovate:Name"] = "config-user",
                ["Tradovate:Password"] = "config-pass",
                ["Tradovate:Cid"] = "1",
                ["Tradovate:Sec"] = "config-sec",
            })
            .Build();
        var service = BuildService(factory, store, configuration);

        await service.SaveTradovateSettingsAsync(new(Name: "db-user", Password: "db-pass", Cid: 42, Sec: "db-sec", DeviceId: null));

        var result = await service.GetTradovateSettingsAsync();

        Assert.True(result.IsConfigured);
        Assert.Equal("db-user", result.Name);
        Assert.Equal(42, result.Cid);
        Assert.Equal("Base de datos (/settings)", result.Source);
    }

    [Fact]
    public async Task GetTradovateSettingsAsync_NoDbValue_FallsBackToConfiguration()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tradovate:Name"] = "config-user",
                ["Tradovate:Password"] = "config-pass",
                ["Tradovate:Cid"] = "7",
                ["Tradovate:Sec"] = "config-sec",
            })
            .Build();
        var service = BuildService(factory, BuildStore(factory), configuration);

        var result = await service.GetTradovateSettingsAsync();

        Assert.True(result.IsConfigured);
        Assert.Equal("config-user", result.Name);
        Assert.Equal("Configuración (User Secrets/entorno)", result.Source);
    }

    [Fact]
    public async Task ClearTradovateSettingsAsync_RemovesDbValues_FallsBackToConfiguration()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var store = BuildStore(factory);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tradovate:Name"] = "config-user",
                ["Tradovate:Password"] = "config-pass",
                ["Tradovate:Cid"] = "7",
                ["Tradovate:Sec"] = "config-sec",
            })
            .Build();
        var service = BuildService(factory, store, configuration);
        await service.SaveTradovateSettingsAsync(new(Name: "db-user", Password: "db-pass", Cid: 42, Sec: "db-sec", DeviceId: null));

        await service.ClearTradovateSettingsAsync();
        var result = await service.GetTradovateSettingsAsync();

        Assert.True(result.IsConfigured);
        Assert.Equal("config-user", result.Name); // vuelve a la config, ya no hay nada en BD
    }

    [Fact]
    public async Task DbTradovateCredentialStore_ReturnsCredentials_WhenAllFieldsPresent()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var store = BuildStore(factory);
        await store.SetAsync(DbTradovateCredentialStore.Key(UserId, "Name"), "trader1");
        await store.SetAsync(DbTradovateCredentialStore.Key(UserId, "Password"), "pw");
        await store.SetAsync(DbTradovateCredentialStore.Key(UserId, "Cid"), "99");
        await store.SetAsync(DbTradovateCredentialStore.Key(UserId, "Sec"), "sec");

        var credStore = new DbTradovateCredentialStore(store);
        var creds = await credStore.GetCredentialsAsync(UserId);

        Assert.NotNull(creds);
        Assert.Equal("trader1", creds!.Name);
        Assert.Equal(99, creds.Cid);
        Assert.False(string.IsNullOrWhiteSpace(creds.DeviceId));
    }

    [Fact]
    public async Task DbTradovateCredentialStore_ReturnsNull_WhenIncomplete()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var store = BuildStore(factory);
        await store.SetAsync(DbTradovateCredentialStore.Key(UserId, "Name"), "trader1"); // falta password/cid/sec

        var credStore = new DbTradovateCredentialStore(store);

        Assert.Null(await credStore.GetCredentialsAsync(UserId));
    }

    [Fact]
    public async Task DbTradovateCredentialStore_DoesNotLeakCredentialsBetweenUsers()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var store = BuildStore(factory);
        await store.SetAsync(DbTradovateCredentialStore.Key("user-a", "Name"), "trader-a");
        await store.SetAsync(DbTradovateCredentialStore.Key("user-a", "Password"), "pw-a");
        await store.SetAsync(DbTradovateCredentialStore.Key("user-a", "Cid"), "1");
        await store.SetAsync(DbTradovateCredentialStore.Key("user-a", "Sec"), "sec-a");

        var credStore = new DbTradovateCredentialStore(store);

        Assert.Null(await credStore.GetCredentialsAsync("user-b"));
    }

    [Fact]
    public async Task TradovateCredentialStore_PrefersDbOverConfiguration()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var store = BuildStore(factory);
        await store.SetAsync(DbTradovateCredentialStore.Key(UserId, "Name"), "db-user");
        await store.SetAsync(DbTradovateCredentialStore.Key(UserId, "Password"), "db-pass");
        await store.SetAsync(DbTradovateCredentialStore.Key(UserId, "Cid"), "1");
        await store.SetAsync(DbTradovateCredentialStore.Key(UserId, "Sec"), "db-sec");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tradovate:Name"] = "config-user",
                ["Tradovate:Password"] = "config-pass",
                ["Tradovate:Cid"] = "2",
                ["Tradovate:Sec"] = "config-sec",
            })
            .Build();

        var composite = new TradovateCredentialStore(
            new DbTradovateCredentialStore(store), new ConfigurationTradovateCredentialStore(configuration));

        var creds = await composite.GetCredentialsAsync(UserId);

        Assert.NotNull(creds);
        Assert.Equal("db-user", creds!.Name);
    }

    [Fact]
    public async Task GetAiSettingsStatusAsync_ReflectsAiOptions()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var service = BuildService(factory, BuildStore(factory), aiConfigured: true);

        var status = await service.GetAiSettingsStatusAsync();

        Assert.True(status.IsConfigured);
    }

    [Fact]
    public async Task IngestSettings_SaveThenClear_HasNoConfigurationFallback()
    {
        // A diferencia de Tradovate, la API key de ingesta no tiene fallback de configuración
        // global: el propio propósito de la búsqueda por valor es resolver DE QUÉ usuario es.
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var store = BuildStore(factory);
        var service = BuildService(factory, store);

        // La clave debe superar la longitud mínima exigida (>= 24 caracteres). [SEC-08]
        await service.SaveIngestApiKeyAsync("db-key-con-longitud-suficiente-1234");
        var afterSave = await service.GetIngestSettingsAsync();
        Assert.True(afterSave.IsConfigured);
        Assert.Equal("Base de datos (/settings)", afterSave.Source);

        await service.ClearIngestApiKeyAsync();
        var afterClear = await service.GetIngestSettingsAsync();
        Assert.False(afterClear.IsConfigured);
    }

    [Fact]
    public async Task SaveIngestApiKey_TooShort_Throws()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var store = BuildStore(factory);
        var service = BuildService(factory, store);

        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveIngestApiKeyAsync("corta"));
    }

    [Fact]
    public async Task GenerateIngestApiKey_ReturnsStrongKeyAndPersists()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var store = BuildStore(factory);
        var service = BuildService(factory, store);

        var key = await service.GenerateIngestApiKeyAsync();

        Assert.True(key.Length >= 24);
        Assert.True((await service.GetIngestSettingsAsync()).IsConfigured);
    }
}
