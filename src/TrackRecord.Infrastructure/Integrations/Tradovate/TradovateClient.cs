using System.Collections.Concurrent;
using System.Net.Http.Json;

namespace TrackRecord.Infrastructure.Integrations.Tradovate;

/// <summary>
/// Cliente REST de Tradovate. La autenticación (Bearer token + renovación) la resuelve
/// TradovateAuthHandler, adjunto como DelegatingHandler al HttpClient inyectado — este
/// cliente no gestiona tokens directamente, solo marca cada petición con el usuario cuyas
/// credenciales debe usar el handler (el HttpClient/handler se comparten entre usuarios).
///
/// NOTA: los endpoints y shapes exactos deben verificarse contra la documentación oficial de
/// Tradovate antes de operar en real (ver GUIA_IMPLEMENTACION.md §5 y Apéndice A) — pueden
/// cambiar entre versiones de su API.
/// </summary>
public class TradovateClient(HttpClient httpClient) : ITradovateClient
{
    private readonly ConcurrentDictionary<long, string> _contractSymbolCache = new();

    public async Task<IReadOnlyList<TradovateAccount>> GetAccountsAsync(string userId, CancellationToken ct = default)
    {
        var raw = await GetJsonAsync<List<TradovateAccountRaw>>(userId, "account/list", ct);
        return raw
            .Select(a => new TradovateAccount(a.Id, a.Name, a.Active))
            .ToList();
    }

    public async Task<IReadOnlyList<TradovateFill>> GetFillsAsync(string userId, long accountId, DateTimeOffset since, CancellationToken ct = default)
    {
        var orders = await GetJsonAsync<List<TradovateOrderRaw>>(userId, "order/list", ct);
        var orderIdToAccountId = orders.ToDictionary(o => o.Id, o => o.AccountId);

        var allFills = await GetJsonAsync<List<TradovateFillRaw>>(userId, "fill/list", ct);

        var relevantFills = allFills
            .Where(f => f.Timestamp >= since)
            .Where(f => orderIdToAccountId.TryGetValue(f.OrderId, out var fillAccountId) && fillAccountId == accountId)
            .ToList();

        var result = new List<TradovateFill>(relevantFills.Count);
        foreach (var fill in relevantFills)
        {
            var symbol = await ResolveContractSymbolAsync(userId, fill.ContractId, ct);
            result.Add(new TradovateFill(fill.Id, symbol, fill.Action, fill.Price, fill.Qty, fill.Timestamp));
        }

        return result;
    }

    private async Task<string> ResolveContractSymbolAsync(string userId, long contractId, CancellationToken ct)
    {
        if (_contractSymbolCache.TryGetValue(contractId, out var cached))
        {
            return cached;
        }

        var contract = await GetJsonAsync<TradovateContractRaw>(userId, $"contract/item?id={contractId}", ct);
        _contractSymbolCache[contractId] = contract.Name;
        return contract.Name;
    }

    private async Task<T> GetJsonAsync<T>(string userId, string relativeUrl, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
        request.Options.Set(TradovateRequestOptions.UserIdKey, userId);

        var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new TradovateApiException($"Tradovate GET {relativeUrl} devolvió {(int)response.StatusCode}: {body}");
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct)
            ?? throw new TradovateApiException($"Tradovate GET {relativeUrl} devolvió una respuesta vacía o no parseable.");
    }
}
