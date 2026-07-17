using System.Text;
using Microsoft.EntityFrameworkCore;
using FundedEdge.Application.Abstractions;
using FundedEdge.Application.Dtos;
using FundedEdge.Infrastructure.Persistence;

namespace FundedEdge.Infrastructure.Services;

/// <summary>
/// Orquesta la sincronización de reglas de una firma (GUIA: automatización de reglas). Descarga las
/// URLs fuente con Nimble, extrae los programas con IA y reutiliza <see cref="IExternalFirmDataProvider"/>
/// para validar el JSON resultante contra el esquema de dominio. Actualiza el reglamento editorial
/// (Markdown) de la firma y devuelve los programas propuestos para revisión del administrador; no
/// los persiste (eso queda a la confirmación explícita en la UI).
/// </summary>
public sealed class FirmRulesSyncService(
    IDbContextFactory<FundedEdgeDbContext> dbFactory,
    INimbleClient nimble,
    IRuleExtractionService extraction,
    IExternalFirmDataProvider externalProvider) : IFirmRulesSyncService
{
    public bool IsConfigured => nimble.IsConfigured && extraction.IsConfigured;

    public async Task<FirmRulesSyncResult> SyncAsync(Guid firmId, CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException(
                "La sincronización automática requiere Nimble (Nimble:ApiKey) y la IA (ANTHROPIC_API_KEY) configurados.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var firm = await db.PropFirms.FirstOrDefaultAsync(f => f.Id == firmId, ct)
            ?? throw new KeyNotFoundException($"Firma {firmId} no encontrada.");

        var urls = (firm.RulesSourceUrls ?? string.Empty)
            .Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(u => u.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList();
        if (urls.Count == 0)
            throw new InvalidOperationException(
                $"La firma «{firm.Name}» no tiene URLs fuente configuradas (campo RulesSourceUrls).");

        var sb = new StringBuilder();
        foreach (var url in urls)
        {
            var content = await nimble.FetchContentAsync(url, ct);
            sb.AppendLine($"<!-- Fuente: {url} -->");
            sb.AppendLine(content);
            sb.AppendLine();
        }
        var markdown = sb.ToString().Trim();

        var json = await extraction.ExtractProgramsJsonAsync(firm.Name, markdown, ct);

        string? warning = null;
        IReadOnlyList<EvaluationProgramDto> programs;
        try
        {
            programs = await externalProvider.FetchProgramsAsync(json, ct);
        }
        catch (Exception ex)
        {
            programs = [];
            warning = $"El reglamento se actualizó, pero la extracción de programas no se pudo validar: {ex.Message}";
        }

        // Se persiste el reglamento editorial recuperado; los programas se guardan tras revisión.
        firm.RulesMarkdown = markdown;
        firm.RulesSource = "Nimble+Claude";
        firm.RulesUpdatedOn = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        await db.SaveChangesAsync(ct);

        return new FirmRulesSyncResult(firm.Id, firm.Name, markdown, programs, warning);
    }
}
