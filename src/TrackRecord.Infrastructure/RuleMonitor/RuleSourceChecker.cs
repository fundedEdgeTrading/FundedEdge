using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TrackRecord.Application.Abstractions;
using TrackRecord.Application.Dtos;
using TrackRecord.Infrastructure.Email;
using TrackRecord.Infrastructure.Persistence;

namespace TrackRecord.Infrastructure.RuleMonitor;

/// <summary>
/// Comprobación de una fuente: fetch → normalizar → SHA-256 → comparar con la línea base.
/// La primera comprobación de una fuente solo establece la línea base (no cuenta como cambio).
/// Si el contenido cambió, actualiza el hash, marca <c>LastChangedAt</c>, lanza la extracción
/// LLM (fase 2; solo cuando hay API key, así el coste en régimen estacionario es casi nulo) y
/// avisa por email al administrador (RuleMonitor:NotifyEmail). Los errores de red no rompen el
/// barrido: se guardan en <c>LastError</c> para que la UI los muestre.
/// </summary>
public class RuleSourceChecker(
    IHttpClientFactory httpClientFactory,
    IDbContextFactory<TrackRecordDbContext> dbFactory,
    IAppEmailSender emailSender,
    IRuleExtractionService extractionService,
    RuleMonitorOptions options,
    ILogger<RuleSourceChecker> logger) : IRuleSourceChecker
{
    public const string HttpClientName = "RuleMonitor";

    public async Task<RuleSourceCheckResult> CheckAsync(Guid ruleSourceId, bool forceExtraction = false, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var source = await db.RuleSources
            .Include(s => s.PropFirm)
            .SingleOrDefaultAsync(s => s.Id == ruleSourceId, ct)
            ?? throw new KeyNotFoundException($"RuleSource {ruleSourceId} no encontrada.");

        source.LastCheckedAt = DateTimeOffset.UtcNow;

        string normalized;
        try
        {
            normalized = await FetchNormalizedContentAsync(source.Url, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            source.LastError = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
            await db.SaveChangesAsync(ct);
            logger.LogWarning("Fallo comprobando la fuente {Url} de {Firm}: {Error}", source.Url, source.PropFirm?.Name, ex.Message);
            return new RuleSourceCheckResult(Changed: false, Error: source.LastError);
        }

        source.LastError = null;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));

        var isBaseline = source.LastContentHash is null;
        var changed = !isBaseline && !string.Equals(source.LastContentHash, hash, StringComparison.OrdinalIgnoreCase);

        source.LastContentHash = hash;
        if (changed)
        {
            source.LastChangedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);

        // Fase 2: solo cuando algo cambió (o se fuerza desde el admin) se paga la extracción LLM.
        var proposals = 0;
        string? extractionError = null;
        if ((changed || forceExtraction) && extractionService.IsConfigured)
        {
            try
            {
                proposals = await extractionService.ProposeFromContentAsync(source.Id, normalized, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                extractionError = ex.Message;
                logger.LogError(ex, "Fallo extrayendo reglas de {Url}.", source.Url);
            }
        }

        if (changed)
        {
            logger.LogInformation("Cambio detectado en la fuente {Url} de {Firm}.", source.Url, source.PropFirm?.Name);
            await NotifyChangeAsync(source.PropFirm?.Name ?? "(firma desconocida)", source.Url, proposals, ct);
        }

        return new RuleSourceCheckResult(changed, Error: null, proposals, extractionError);
    }

    /// <summary>Descarga la página y la reduce a texto visible, que es lo que se hashea.</summary>
    internal async Task<string> FetchNormalizedContentAsync(string url, CancellationToken ct)
    {
        var http = httpClientFactory.CreateClient(HttpClientName);
        var html = await http.GetStringAsync(url, ct);
        return HtmlContentNormalizer.Normalize(html);
    }

    private async Task NotifyChangeAsync(string firmName, string url, int proposals, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(options.NotifyEmail)) return;

        var nextStep = proposals > 0
            ? $"<p>La extracción automática ha dejado <strong>{proposals} propuesta(s)</strong> de cambio pendientes de revisión en <strong>/admin/rule-monitor</strong>.</p>"
            : "<p>Revisa el catálogo en <strong>/admin/rule-monitor</strong> y actualiza los programas afectados si procede.</p>";

        var subject = $"[FundedEdge] Cambio de reglas detectado: {firmName}";
        var body =
            $"""
            <p>El monitor de reglas ha detectado un cambio en una página oficial de <strong>{WebUtility.HtmlEncode(firmName)}</strong>:</p>
            <p><a href="{WebUtility.HtmlEncode(url)}">{WebUtility.HtmlEncode(url)}</a></p>
            {nextStep}
            """;

        try
        {
            await emailSender.SendAsync(options.NotifyEmail!, subject, body, ct);
        }
        catch (Exception ex)
        {
            // El aviso es best-effort: el cambio ya queda registrado en LastChangedAt y en la UI.
            logger.LogError(ex, "No se pudo enviar el aviso de cambio de reglas de {Firm}.", firmName);
        }
    }
}
