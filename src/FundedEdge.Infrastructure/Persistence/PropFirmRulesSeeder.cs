using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FundedEdge.Domain.Entities;

namespace FundedEdge.Infrastructure.Persistence;

/// <summary>
/// Carga por defecto el reglamento completo (Markdown) de cada prop firm desde los .md incluidos
/// como recursos embebidos (Resources/PropFirmRules) hacia <see cref="PropFirm.RulesMarkdown"/>, y
/// da de alta las firmas presentes en esos .md que aún no estaban en el catálogo original
/// (Alpha Futures, FundedNext).
///
/// Idempotente y no destructivo: solo refresca el reglamento cuando su origen sigue siendo "Seed",
/// de modo que nunca pisa las actualizaciones hechas por el pipeline automatizado (Nimble + IA) ni
/// las ediciones manuales del administrador. Se invoca en el arranque, tras aplicar migraciones.
/// </summary>
public class PropFirmRulesSeeder(
    IDbContextFactory<FundedEdgeDbContext> dbFactory,
    ILogger<PropFirmRulesSeeder> logger)
{
    public const string SeedSource = "Seed";

    // Firmas presentes en los .md adjuntos pero ausentes del seed original.
    public static readonly Guid AlphaFuturesId = Guid.Parse("88888888-8888-8888-8888-888888888888");
    public static readonly Guid FundedNextId   = Guid.Parse("99999999-9999-9999-9999-999999999999");

    private static readonly FirmRules[] Catalog =
    [
        new(SeedData.ApexId,             "apex-reglas-2026-07.md",             null,            null),
        new(SeedData.TradeifyId,         "tradeify-reglas-2026-07.md",         null,            null),
        new(SeedData.LucidTradingId,     "lucid-reglas-2026-07.md",            null,            null),
        new(SeedData.MyFundedFuturesId,  "mffu-reglas-2026-07.md",             null,            null),
        new(SeedData.TakeProfitTraderId, "takeprofittrader-reglas-2026-07.md", null,            null),
        new(SeedData.Earn2TradeId,       "earn2trade-reglas-2026-07.md",       null,            null),
        new(AlphaFuturesId,              "alpha-futures-reglas-2026-07.md",    "Alpha Futures", "https://alpha-futures.com"),
        new(FundedNextId,                "fundednext-futures-reglas-2026-07.md", "FundedNext",  "https://fundednext.com"),
    ];

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var changed = 0;

        foreach (var item in Catalog)
        {
            var markdown = ReadResource(item.FileName);
            if (markdown is null)
            {
                logger.LogWarning("Recurso de reglas no encontrado: {File}", item.FileName);
                continue;
            }

            var firm = await db.PropFirms.FirstOrDefaultAsync(f => f.Id == item.FirmId, ct);
            if (firm is null)
            {
                if (item.NewFirmName is null) continue; // firma esperada del seed; no debería faltar
                firm = new PropFirm { Id = item.FirmId, Name = item.NewFirmName, Website = item.NewFirmWebsite };
                db.PropFirms.Add(firm);
            }

            // Solo se refrescan los reglamentos cuyo origen sigue siendo el seed: así el pipeline
            // Nimble+IA y las ediciones manuales quedan protegidos frente a este seeder.
            var isSeedManaged = string.IsNullOrEmpty(firm.RulesSource) || firm.RulesSource == SeedSource;
            if (isSeedManaged && firm.RulesMarkdown != markdown)
            {
                firm.RulesMarkdown = markdown;
                firm.RulesSource = SeedSource;
                firm.RulesUpdatedOn = today;
                changed++;
            }
        }

        if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Reglamentos de prop firms cargados por defecto ({Count} actualizados).", changed);
        }
    }

    private static string? ReadResource(string fileName)
    {
        var asm = typeof(PropFirmRulesSeeder).Assembly;
        var name = Array.Find(asm.GetManifestResourceNames(),
            n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
        if (name is null) return null;
        using var stream = asm.GetManifestResourceStream(name);
        if (stream is null) return null;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private sealed record FirmRules(Guid FirmId, string FileName, string? NewFirmName, string? NewFirmWebsite);
}
