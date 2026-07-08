using System.Globalization;
using TrackRecord.Domain.Enums;

namespace TrackRecord.Domain.Common;

/// <summary>
/// Formatea importes según la divisa de visualización elegida por el usuario. No hay conversión
/// de tipo de cambio: todos los importes se guardan tal cual se introducen y solo cambia el
/// símbolo/formato con el que se muestran (USD: $1,234.56 · EUR: 1.234,56 €).
/// </summary>
public static class CurrencyFormatter
{
    private static readonly CultureInfo UsdCulture = CultureInfo.GetCultureInfo("en-US");

    public static CultureInfo GetCulture(Currency currency) => currency switch
    {
        Currency.Eur => GetEurCulture(),
        _ => UsdCulture,
    };

    private static CultureInfo GetEurCulture()
    {
        var currentLanguage = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        return currentLanguage == "en"
            ? CultureInfo.GetCultureInfo("en-IE")
            : CultureInfo.GetCultureInfo("es-ES");
    }

    public static string Symbol(Currency currency) => currency == Currency.Eur ? "€" : "$";

    public static string Format(decimal value, Currency currency, int decimals = 2) =>
        value.ToString(decimals <= 0 ? "C0" : "C2", GetCulture(currency));

    public static string Format(decimal? value, Currency currency, int decimals = 2) =>
        value is null ? "—" : Format(value.Value, currency, decimals);
}
