using TrackRecord.Domain.Enums;

namespace TrackRecord.Infrastructure.Billing;

/// <summary>
/// Mapeo bidireccional entre los 4 Price Id de Stripe (Pro/Elite × mensual/anual) y el
/// PlanTier correspondiente. Único sitio donde vive esta correspondencia — ver
/// GUIA_MONETIZACION_Y_MARKETING.md §6 (F4).
/// </summary>
public sealed class StripePriceCatalog
{
    private readonly Dictionary<string, PlanTier> _priceToTier = [];
    private readonly Dictionary<(PlanTier Tier, bool Yearly), string> _tierToPrice = [];

    public StripePriceCatalog(string? proMonthly, string? proYearly, string? eliteMonthly, string? eliteYearly)
    {
        Add(PlanTier.Pro, yearly: false, proMonthly);
        Add(PlanTier.Pro, yearly: true, proYearly);
        Add(PlanTier.Elite, yearly: false, eliteMonthly);
        Add(PlanTier.Elite, yearly: true, eliteYearly);
    }

    public string? GetPriceId(PlanTier tier, bool yearly) => _tierToPrice.GetValueOrDefault((tier, yearly));

    public PlanTier? GetTier(string priceId) => _priceToTier.TryGetValue(priceId, out var tier) ? tier : null;

    private void Add(PlanTier tier, bool yearly, string? priceId)
    {
        if (string.IsNullOrWhiteSpace(priceId)) return;
        _tierToPrice[(tier, yearly)] = priceId;
        _priceToTier[priceId] = tier;
    }
}
