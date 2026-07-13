namespace FundedEdge.Domain.Common;

/// <summary>
/// Marca centralizada. El nombre definitivo es "FundedEdge" (ver
/// GUIA_SEO_MARKETING_REBRANDING.md §1) — todo el producto (títulos, emails, PDF, landing,
/// metadatos SEO) lo hereda de aquí; no hardcodear el nombre en ningún otro sitio.
/// </summary>
public static class Brand
{
    public const string Name = "FundedEdge";

    /// <summary>Eslogan por defecto (ES). La clave está localizada en SharedResources (EN incluido).</summary>
    public const string Tagline = "El copiloto financiero del trader de fondeo";

    /// <summary>Descripción corta para metadatos SEO/Open Graph (ES). Localizable vía SharedResources.</summary>
    public const string Description =
        "Cuentas, evaluaciones, payouts y métricas de riesgo de todas tus firmas de fondeo, en un único dashboard. Sabrás si tu negocio de evaluaciones es rentable — no lo intuirás.";
}
