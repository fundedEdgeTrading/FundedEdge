using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace TrackRecord.Infrastructure.Integrations.Tradovate;

/// <summary>
/// DelegatingHandler que añade el Bearer token a cada petición a la API de Tradovate,
/// autenticando la primera vez y renovando el token de forma transparente antes de que
/// expire (ver GUIA_IMPLEMENTACION.md §5). Usa un HttpClient nombrado independiente
/// ("TradovateAuthRaw", sin este mismo handler adjunto) para las llamadas de auth/renovación,
/// evitando así una recursión infinita sobre el propio pipeline.
///
/// El handler se comparte entre usuarios (vive en el pool de IHttpClientFactory), así que el
/// token se cachea por usuario — TradovateClient marca cada petición con
/// TradovateRequestOptions.UserIdKey antes de enviarla.
/// </summary>
public class TradovateAuthHandler(
    IHttpClientFactory httpClientFactory,
    ITradovateCredentialStore credentialStore,
    ILogger<TradovateAuthHandler> logger) : DelegatingHandler
{
    private static readonly TimeSpan RenewMargin = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly ConcurrentDictionary<string, CachedToken> _tokensByUser = new();

    private sealed record CachedToken(string AccessToken, DateTimeOffset ExpiresAt);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!request.Options.TryGetValue(TradovateRequestOptions.UserIdKey, out var userId) || string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Petición a Tradovate sin usuario asociado; se enviará sin autenticar.");
            return await base.SendAsync(request, cancellationToken);
        }

        var token = await GetValidTokenAsync(userId, cancellationToken);
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string?> GetValidTokenAsync(string userId, CancellationToken ct)
    {
        if (HasValidCachedToken(userId, out var cached))
        {
            return cached;
        }

        var userLock = _locks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        await userLock.WaitAsync(ct);
        try
        {
            // Doble comprobación: otra petición concurrente del mismo usuario pudo haber renovado
            // ya el token mientras esperábamos el lock.
            if (HasValidCachedToken(userId, out cached))
            {
                return cached;
            }

            var credentials = await credentialStore.GetCredentialsAsync(userId, ct);
            if (credentials is null)
            {
                logger.LogWarning("No hay credenciales de Tradovate configuradas para el usuario {UserId}; la petición se enviará sin autenticar y probablemente será rechazada.", userId);
                return null;
            }

            var response = await AuthenticateAsync(credentials, ct);
            var expiresAt = response.ExpirationTime ?? DateTimeOffset.UtcNow.AddMinutes(70);
            _tokensByUser[userId] = new CachedToken(response.AccessToken!, expiresAt);
            return response.AccessToken;
        }
        finally
        {
            userLock.Release();
        }
    }

    private bool HasValidCachedToken(string userId, out string? token)
    {
        if (_tokensByUser.TryGetValue(userId, out var cached) && DateTimeOffset.UtcNow < cached.ExpiresAt - RenewMargin)
        {
            token = cached.AccessToken;
            return true;
        }

        token = null;
        return false;
    }

    private async Task<TradovateAuthResponse> AuthenticateAsync(TradovateCredentials credentials, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("TradovateAuthRaw");
        var request = new TradovateAuthRequest(
            credentials.Name, credentials.Password, "TrackRecord", "1.0",
            credentials.Cid, credentials.Sec, credentials.DeviceId);

        var httpResponse = await client.PostAsJsonAsync("auth/accesstokenrequest", request, ct);
        var body = await httpResponse.Content.ReadFromJsonAsync<TradovateAuthResponse>(cancellationToken: ct)
            ?? throw new TradovateApiException("Respuesta vacía o inválida de Tradovate al autenticar.");

        if (body.PTicket is not null && body.PTime is >= 0)
        {
            // Rate limit: Tradovate exige esperar p-time segundos antes de reintentar. Se
            // respeta una sola vez para no bloquear indefinidamente el arranque del sync.
            logger.LogWarning("Tradovate ha aplicado rate limiting al login (p-ticket). Esperando {Seconds}s antes de reintentar.", body.PTime);
            await Task.Delay(TimeSpan.FromSeconds(body.PTime.Value), ct);

            httpResponse = await client.PostAsJsonAsync("auth/accesstokenrequest", request, ct);
            body = await httpResponse.Content.ReadFromJsonAsync<TradovateAuthResponse>(cancellationToken: ct)
                ?? throw new TradovateApiException("Respuesta vacía o inválida de Tradovate al autenticar (tras reintento por rate limit).");
        }

        if (string.IsNullOrWhiteSpace(body.AccessToken))
        {
            throw new TradovateApiException($"Autenticación con Tradovate rechazada: {body.ErrorText ?? "sin detalle"}.");
        }

        return body;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var userLock in _locks.Values) userLock.Dispose();
        }
        base.Dispose(disposing);
    }
}
