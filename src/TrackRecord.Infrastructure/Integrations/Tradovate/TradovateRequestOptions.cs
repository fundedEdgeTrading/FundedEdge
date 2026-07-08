namespace TrackRecord.Infrastructure.Integrations.Tradovate;

/// <summary>
/// Clave usada para marcar cada HttpRequestMessage con el usuario cuyas credenciales debe usar
/// TradovateAuthHandler — el handler se comparte entre peticiones de distintos usuarios (vive en
/// el pool de IHttpClientFactory), así que no puede inferirlo de un estado propio.
/// </summary>
internal static class TradovateRequestOptions
{
    public static readonly HttpRequestOptionsKey<string> UserIdKey = new("TrackRecord.Tradovate.UserId");
}
