using TrackRecord.Application.Abstractions;
using TrackRecord.Application.Dtos;
using TrackRecord.Domain.Enums;
using TrackRecord.Infrastructure.Settings;

namespace TrackRecord.Web.Endpoints;

/// <summary>
/// Endpoint de ingesta para el AddOn de NinjaTrader 8 (ver GUIA_IMPLEMENTACION.md §6 e
/// integrations/ninjatrader/TrackRecordExporter.cs). Protegido por una API key propia de cada
/// usuario ("{userId}:Ingest:NinjaTraderApiKey", configurable desde /settings o vía User
/// Secrets/entorno — nunca en appsettings.json): la clave entrante se busca entre las de todos
/// los usuarios para resolver a quién pertenece, y solo se ingiere en cuentas de ese usuario.
/// [AllowAnonymous] porque se autentica con su propia API key, no con la cookie de sesión.
/// Recomendado: enlazar Kestrel solo a localhost cuando NT8 corre en la misma máquina.
/// </summary>
public static class NinjaTraderIngestEndpoints
{
    private const string ApiKeySuffix = ":Ingest:NinjaTraderApiKey";

    public static IEndpointRouteBuilder MapNinjaTraderIngestEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/ingest/ninjatrader/executions", HandleIngestAsync)
            .AllowAnonymous()
            .RequireRateLimiting("ingest"); // [SEC-02/SEC-08] limita la fuerza bruta de la API key
        return app;
    }

    private static async Task<IResult> HandleIngestAsync(
        NtExecutionRequest request,
        HttpRequest httpRequest,
        IExecutionIngestService ingestService,
        IIntegrationSettingsStore settingsStore,
        CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(httpRequest, settingsStore, ct);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (!TryMapSide(request.Side, out var side))
        {
            return Results.BadRequest($"Side inválido: '{request.Side}'. Se esperaba \"Long\" o \"Short\".");
        }

        var result = await ingestService.IngestAsync(
            new IngestExecutionRequest(
                request.ExternalId,
                TradeSourceType.NinjaTraderAddOn,
                request.AccountName,
                request.Symbol,
                side,
                request.Quantity,
                request.Price,
                request.ExecutedAt,
                request.Commission),
            userId,
            ct);

        if (!result.AccountResolved)
        {
            // 422: la petición está bien formada pero no hay ninguna TradingAccount de este
            // usuario cuyo ExternalAccountId coincida con accountName — hace falta configurarla en /settings.
            return Results.UnprocessableEntity($"No hay ninguna cuenta configurada con ExternalAccountId = '{request.AccountName}'.");
        }

        return Results.Accepted(value: result);
    }

    /// <summary>
    /// Resuelve el usuario dueño de la API key recibida buscando entre las que cada uno haya
    /// guardado desde /settings — es la única fuente para este endpoint (a diferencia de
    /// Tradovate, aquí no tiene sentido un fallback de configuración global: el propósito mismo
    /// de la búsqueda es averiguar A QUÉ usuario pertenece la clave).
    /// </summary>
    private static Task<string?> ResolveUserIdAsync(HttpRequest request, IIntegrationSettingsStore settingsStore, CancellationToken ct)
    {
        if (!request.Headers.TryGetValue("X-Api-Key", out var providedKey) || string.IsNullOrWhiteSpace(providedKey))
        {
            return Task.FromResult<string?>(null);
        }

        return settingsStore.FindKeyPrefixByValueAsync(ApiKeySuffix, providedKey.ToString(), ct);
    }

    private static bool TryMapSide(string ntMarketPosition, out OrderSide side)
    {
        // NT8 reporta Execution.MarketPosition como "Long"/"Short" para indicar el sentido del
        // propio fill (no la posición resultante) — ver comentario en TrackRecordExporter.cs.
        switch (ntMarketPosition)
        {
            case "Long":
                side = OrderSide.Buy;
                return true;
            case "Short":
                side = OrderSide.Sell;
                return true;
            default:
                side = default;
                return false;
        }
    }
}

/// <summary>Payload que envía TrackRecordExporter.cs (el AddOn de NinjaTrader 8) por cada fill.</summary>
public record NtExecutionRequest(
    string ExternalId,
    string AccountName,
    string Symbol,
    string Side,
    int Quantity,
    decimal Price,
    DateTimeOffset ExecutedAt,
    decimal Commission);
