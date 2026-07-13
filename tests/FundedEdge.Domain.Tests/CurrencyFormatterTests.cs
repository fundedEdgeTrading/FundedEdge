using FundedEdge.Domain.Common;
using FundedEdge.Domain.Enums;

namespace FundedEdge.Domain.Tests;

public class CurrencyFormatterTests
{
    [Fact]
    public void Format_Usd_UsesDollarSymbolAndPeriodDecimal() =>
        Assert.Equal("$1,234.56", CurrencyFormatter.Format(1234.56m, Currency.Usd));

    [Fact]
    public void Format_Eur_UsesEuroSymbolAndCommaDecimal() =>
        Assert.Equal("1.234,56 €", CurrencyFormatter.Format(1234.56m, Currency.Eur));

    [Fact]
    public void Format_ZeroDecimals_RoundsToWholeUnits() =>
        Assert.Equal("$1,235", CurrencyFormatter.Format(1234.56m, Currency.Usd, decimals: 0));

    [Fact]
    public void Format_NullableNull_ReturnsEmDash() =>
        Assert.Equal("—", CurrencyFormatter.Format((decimal?)null, Currency.Usd));

    [Fact]
    public void Format_NullableWithValue_FormatsNormally() =>
        Assert.Equal("$50", CurrencyFormatter.Format((decimal?)50m, Currency.Usd, decimals: 0));

    [Theory]
    [InlineData(Currency.Usd, "$")]
    [InlineData(Currency.Eur, "€")]
    public void Symbol_ReturnsExpectedSymbol(Currency currency, string expected) =>
        Assert.Equal(expected, CurrencyFormatter.Symbol(currency));
}
