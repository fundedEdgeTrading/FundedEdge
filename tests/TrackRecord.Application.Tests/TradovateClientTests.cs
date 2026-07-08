using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using TrackRecord.Infrastructure.Integrations.Tradovate;

namespace TrackRecord.Application.Tests;

/// <summary>Sustituye el HttpMessageHandler real por respuestas grabadas, sin tocar la red.</summary>
public sealed class FakeTradovateHandler : HttpMessageHandler
{
    public int AuthCallCount { get; private set; }

    private bool _rateLimitFirstAuthAttempt;

    public static FakeTradovateHandler WithRateLimitOnFirstAuth() =>
        new() { _rateLimitFirstAuthAttempt = true };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath;

        if (path.EndsWith("auth/accesstokenrequest", StringComparison.Ordinal))
        {
            AuthCallCount++;

            if (_rateLimitFirstAuthAttempt && AuthCallCount == 1)
            {
                // p-ticket/p-time llevan guiones, no válidos como nombres de propiedad anónima en
                // C#; se serializan vía diccionario con sus nombres reales de wire.
                var rateLimited = new Dictionary<string, object?> { ["p-ticket"] = "abc", ["p-time"] = 0 };
                return Task.FromResult(JsonResponse(rateLimited));
            }

            return Task.FromResult(JsonResponse(new
            {
                accessToken = "fake-token",
                expirationTime = DateTimeOffset.UtcNow.AddMinutes(70),
            }));
        }

        if (path.EndsWith("account/list", StringComparison.Ordinal))
        {
            return Task.FromResult(JsonResponse(new[] { new { id = 1L, name = "DEMO1", active = true } }));
        }

        if (path.EndsWith("order/list", StringComparison.Ordinal))
        {
            return Task.FromResult(JsonResponse(new[] { new { id = 100L, accountId = 1L } }));
        }

        if (path.EndsWith("fill/list", StringComparison.Ordinal))
        {
            return Task.FromResult(JsonResponse(new[]
            {
                new { id = 500L, orderId = 100L, contractId = 999L, timestamp = new DateTimeOffset(2026, 1, 5, 14, 0, 0, TimeSpan.Zero), action = "Buy", price = 5000m, qty = 1 },
                new { id = 501L, orderId = 999L, contractId = 999L, timestamp = new DateTimeOffset(2026, 1, 5, 14, 5, 0, TimeSpan.Zero), action = "Sell", price = 5010m, qty = 1 }, // orden de otra cuenta: debe filtrarse
            }));
        }

        if (path.EndsWith("contract/item", StringComparison.Ordinal))
        {
            return Task.FromResult(JsonResponse(new { id = 999L, name = "ESH6" }));
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    private static HttpResponseMessage JsonResponse<T>(T body) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(body) };
}

public class TradovateClientTests
{
    private const string UserId = "user-1";

    private sealed class FakeCredentialStore(TradovateCredentials? credentials) : ITradovateCredentialStore
    {
        public Task<TradovateCredentials?> GetCredentialsAsync(string userId, CancellationToken ct = default) => Task.FromResult(credentials);
    }

    private static (ITradovateClient Client, FakeTradovateHandler Handler) BuildClient(TradovateCredentials? credentials, FakeTradovateHandler? handler = null)
    {
        handler ??= new FakeTradovateHandler();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITradovateCredentialStore>(new FakeCredentialStore(credentials));
        services.AddTransient<TradovateAuthHandler>();

        services.AddHttpClient("TradovateAuthRaw", c => c.BaseAddress = new Uri("https://fake.tradovateapi.com/v1/"))
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        services.AddHttpClient<ITradovateClient, TradovateClient>(c => c.BaseAddress = new Uri("https://fake.tradovateapi.com/v1/"))
            .AddHttpMessageHandler<TradovateAuthHandler>()
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        var provider = services.BuildServiceProvider();
        return (provider.GetRequiredService<ITradovateClient>(), handler);
    }

    private static readonly TradovateCredentials Credentials = new("user", "pass", 123, "secret", "device-1");

    [Fact]
    public async Task GetAccountsAsync_AuthenticatesAndMapsAccounts()
    {
        var (client, handler) = BuildClient(Credentials);

        var accounts = await client.GetAccountsAsync(UserId);

        Assert.Equal(1, handler.AuthCallCount);
        var account = Assert.Single(accounts);
        Assert.Equal(1L, account.Id);
        Assert.Equal("DEMO1", account.Name);
        Assert.True(account.Active);
    }

    [Fact]
    public async Task GetAccountsAsync_CalledTwice_ReusesTheCachedToken()
    {
        var (client, handler) = BuildClient(Credentials);

        await client.GetAccountsAsync(UserId);
        await client.GetAccountsAsync(UserId);

        Assert.Equal(1, handler.AuthCallCount); // el token cacheado se reutiliza, no se re-autentica
    }

    [Fact]
    public async Task GetAccountsAsync_NoCredentialsConfigured_SendsRequestWithoutToken()
    {
        var (client, handler) = BuildClient(credentials: null);

        var accounts = await client.GetAccountsAsync(UserId);

        Assert.Equal(0, handler.AuthCallCount);
        Assert.Single(accounts); // el fake responde igualmente (la API real devolvería 401)
    }

    [Fact]
    public async Task GetAccountsAsync_RateLimitedOnFirstAuthAttempt_RetriesAndSucceeds()
    {
        var handler = FakeTradovateHandler.WithRateLimitOnFirstAuth();
        var (client, _) = BuildClient(Credentials, handler);

        var accounts = await client.GetAccountsAsync(UserId);

        Assert.Equal(2, handler.AuthCallCount); // 1er intento -> p-ticket, 2º intento -> token válido
        Assert.Single(accounts);
    }

    [Fact]
    public async Task GetFillsAsync_ResolvesContractSymbolAndFiltersByAccount()
    {
        var (client, _) = BuildClient(Credentials);

        var fills = await client.GetFillsAsync(UserId, accountId: 1, since: DateTimeOffset.MinValue);

        var fill = Assert.Single(fills); // el fill de la orden 999 (otra cuenta) se filtra fuera
        Assert.Equal(500L, fill.Id);
        Assert.Equal("ESH6", fill.Symbol); // contractId 999 resuelto vía contract/item
        Assert.Equal("Buy", fill.Action);
        Assert.Equal(5000m, fill.Price);
        Assert.Equal(1, fill.Quantity);
    }

    [Fact]
    public async Task GetFillsAsync_FiltersOutFillsBeforeSince()
    {
        var (client, _) = BuildClient(Credentials);

        var since = new DateTimeOffset(2026, 1, 5, 14, 30, 0, TimeSpan.Zero); // posterior a ambos fills grabados
        var fills = await client.GetFillsAsync(UserId, accountId: 1, since: since);

        Assert.Empty(fills);
    }
}
