using TrackRecord.Domain.Enums;
using TrackRecord.Infrastructure.Billing;

namespace TrackRecord.Application.Tests;

public class StripePriceCatalogTests
{
    private static readonly StripePriceCatalog Catalog = new(
        proMonthly: "price_pro_monthly",
        proYearly: "price_pro_yearly",
        eliteMonthly: "price_elite_monthly",
        eliteYearly: "price_elite_yearly");

    [Theory]
    [InlineData(PlanTier.Pro, false, "price_pro_monthly")]
    [InlineData(PlanTier.Pro, true, "price_pro_yearly")]
    [InlineData(PlanTier.Elite, false, "price_elite_monthly")]
    [InlineData(PlanTier.Elite, true, "price_elite_yearly")]
    public void GetPriceId_ReturnsTheConfiguredPriceForEachTierAndCadence(PlanTier tier, bool yearly, string expectedPriceId)
    {
        Assert.Equal(expectedPriceId, Catalog.GetPriceId(tier, yearly));
    }

    [Fact]
    public void GetPriceId_Starter_ReturnsNull()
    {
        Assert.Null(Catalog.GetPriceId(PlanTier.Starter, yearly: false));
    }

    [Theory]
    [InlineData("price_pro_monthly", PlanTier.Pro)]
    [InlineData("price_pro_yearly", PlanTier.Pro)]
    [InlineData("price_elite_monthly", PlanTier.Elite)]
    [InlineData("price_elite_yearly", PlanTier.Elite)]
    public void GetTier_ResolvesTheTierForAKnownPrice(string priceId, PlanTier expectedTier)
    {
        Assert.Equal(expectedTier, Catalog.GetTier(priceId));
    }

    [Fact]
    public void GetTier_UnknownPrice_ReturnsNull()
    {
        Assert.Null(Catalog.GetTier("price_does_not_exist"));
    }

    [Fact]
    public void MissingPriceConfiguration_IsIgnoredWithoutThrowing()
    {
        var partial = new StripePriceCatalog(proMonthly: "price_pro_monthly", proYearly: null, eliteMonthly: null, eliteYearly: null);

        Assert.Equal("price_pro_monthly", partial.GetPriceId(PlanTier.Pro, yearly: false));
        Assert.Null(partial.GetPriceId(PlanTier.Pro, yearly: true));
        Assert.Null(partial.GetPriceId(PlanTier.Elite, yearly: false));
    }
}
