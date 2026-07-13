using System.Text;
using Microsoft.EntityFrameworkCore;
using FundedEdge.Infrastructure.Persistence;

namespace FundedEdge.Web.Endpoints;

/// <summary>
/// Endpoints de SEO técnico (ver GUIA_SEO_MARKETING_REBRANDING.md §2.1): robots.txt y sitemap.xml
/// dinámicos. Se sirven desde código (no como estáticos) para inyectar la URL base absoluta —
/// robots exige la del sitemap absoluta y el sitemap las URLs absolutas— y para listar los track
/// records públicos activos en cada momento. Anónimos: son para los rastreadores.
/// </summary>
public static class SeoEndpoints
{
    // Rutas públicas indexables (sin las de /Account, que se excluyen en robots).
    private static readonly string[] PublicPaths = ["/bienvenida", "/precios", "/calculadora"];

    public static IEndpointRouteBuilder MapSeoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/robots.txt", (HttpContext http, IConfiguration configuration) =>
        {
            var baseUrl = ResolveBaseUrl(http, configuration);
            var body = string.Join('\n',
                "User-agent: *",
                "Allow: /",
                "Disallow: /Account",
                "Disallow: /settings",
                "Disallow: /api",
                $"Sitemap: {baseUrl}/sitemap.xml",
                "");
            return Results.Text(body, "text/plain");
        }).AllowAnonymous();

        app.MapGet("/sitemap.xml", async (
            HttpContext http,
            IConfiguration configuration,
            IDbContextFactory<FundedEdgeDbContext> dbFactory,
            CancellationToken ct) =>
        {
            var baseUrl = ResolveBaseUrl(http, configuration);

            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var slugs = await db.PublicProfiles
                .AsNoTracking()
                .Where(p => p.IsEnabled)
                .Select(p => p.Slug)
                .ToListAsync(ct);

            var sb = new StringBuilder();
            sb.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
            sb.AppendLine("""<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">""");

            foreach (var path in PublicPaths)
            {
                AppendUrl(sb, $"{baseUrl}{path}");
            }
            foreach (var slug in slugs)
            {
                AppendUrl(sb, $"{baseUrl}/t/{Uri.EscapeDataString(slug)}");
            }

            sb.AppendLine("</urlset>");
            return Results.Text(sb.ToString(), "application/xml");
        }).AllowAnonymous();

        return app;
    }

    private static void AppendUrl(StringBuilder sb, string location)
    {
        sb.Append("  <url><loc>");
        sb.Append(System.Security.SecurityElement.Escape(location));
        sb.AppendLine("</loc></url>");
    }

    /// <summary>
    /// URL base absoluta. Prioriza App:BaseUrl (configuración de confianza) sobre el Host de la
    /// petición — mismo criterio [SEC-09] que los retornos de Stripe.
    /// </summary>
    private static string ResolveBaseUrl(HttpContext context, IConfiguration configuration)
    {
        var configured = configuration["App:BaseUrl"];
        return !string.IsNullOrWhiteSpace(configured)
            ? configured.TrimEnd('/')
            : $"{context.Request.Scheme}://{context.Request.Host}";
    }
}
