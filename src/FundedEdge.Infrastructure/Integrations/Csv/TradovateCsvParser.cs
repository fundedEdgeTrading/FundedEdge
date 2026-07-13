using System.Globalization;

namespace FundedEdge.Infrastructure.Integrations.Csv;

/// <summary>
/// Parsea el CSV de rendimiento exportado desde la app de Tradovate (web o desktop):
/// Reports → Performance → botón de descarga (CSV), un round-turn por fila.
///
/// Cabecera real del export (formato estable, en inglés y con InvariantCulture):
///   symbol,_priceFormat,_priceFormatType,_tickSize,buyFillId,sellFillId,qty,buyPrice,
///   sellPrice,pnl,boughtTimestamp,soldTimestamp,duration
///
/// Particularidades:
///  - No hay columna de dirección: se infiere del orden temporal — si se compró antes de vender
///    es un Long; si se vendió antes de comprar (venta en corto), un Short.
///  - "pnl" viene formateado como divisa: "$21.25" o "$(12.50)" para negativos.
///  - Timestamps "MM/dd/yyyy HH:mm:ss".
///  - No incluye comisiones ni MAE/MFE (Tradovate las liquida a nivel de cuenta, no por trade).
/// </summary>
public class TradovateCsvParser
{
    private static readonly string[] SymbolHeaders = ["symbol"];
    private static readonly string[] QtyHeaders = ["qty"];
    private static readonly string[] BuyPriceHeaders = ["buyPrice"];
    private static readonly string[] SellPriceHeaders = ["sellPrice"];
    private static readonly string[] PnlHeaders = ["pnl"];
    private static readonly string[] BoughtTsHeaders = ["boughtTimestamp"];
    private static readonly string[] SoldTsHeaders = ["soldTimestamp"];

    private static readonly string[] TimestampFormats =
    [
        "MM/dd/yyyy HH:mm:ss",
        "MM/dd/yyyy HH:mm",
        "yyyy-MM-dd HH:mm:ss",
    ];

    /// <summary>Heurística de detección: las columnas boughtTimestamp/soldTimestamp solo existen en el export de Tradovate.</summary>
    public static bool LooksLikeHeader(IReadOnlyList<string> headers)
    {
        bool Has(string name) => headers.Any(h => string.Equals(h.Trim(), name, StringComparison.OrdinalIgnoreCase));
        return Has("boughtTimestamp") && Has("soldTimestamp");
    }

    public CsvParseResult Parse(TextReader reader)
    {
        var headerLine = reader.ReadLine();
        if (headerLine is null)
        {
            return new CsvParseResult([], [new CsvParseError(1, "", "El archivo está vacío.")]);
        }

        var headers = CsvLineSplitter.Split(headerLine);
        var columnIndex = BuildColumnIndex(headers);

        var missing = new List<string>();
        int Require(string[] aliases, string label)
        {
            var idx = ResolveColumn(columnIndex, aliases);
            if (idx < 0) missing.Add(label);
            return idx;
        }

        var symbolCol = Require(SymbolHeaders, "symbol");
        var qtyCol = Require(QtyHeaders, "qty");
        var buyPriceCol = Require(BuyPriceHeaders, "buyPrice");
        var sellPriceCol = Require(SellPriceHeaders, "sellPrice");
        var pnlCol = Require(PnlHeaders, "pnl");
        var boughtTsCol = Require(BoughtTsHeaders, "boughtTimestamp");
        var soldTsCol = Require(SoldTsHeaders, "soldTimestamp");

        if (missing.Count > 0)
        {
            return new CsvParseResult([], [new CsvParseError(1, headerLine, $"Faltan columnas obligatorias: {string.Join(", ", missing)}.")]);
        }

        var rows = new List<CsvTradeRow>();
        var errors = new List<CsvParseError>();
        var lineNumber = 1;
        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var fields = CsvLineSplitter.Split(line);
            try
            {
                var symbol = Field(fields, symbolCol).Trim();
                var qty = int.Parse(Field(fields, qtyCol).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture);
                var buyPrice = ParseDecimal(Field(fields, buyPriceCol));
                var sellPrice = ParseDecimal(Field(fields, sellPriceCol));
                var pnl = ParsePnl(Field(fields, pnlCol));
                var boughtAt = ParseTimestamp(Field(fields, boughtTsCol));
                var soldAt = ParseTimestamp(Field(fields, soldTsCol));

                // Long: se compra primero y se vende después. Short: la venta (en corto) precede
                // a la recompra. Con timestamps idénticos (scalping en el mismo segundo) se asume
                // Long: sin más datos es indistinguible y no afecta al P&L.
                var isLong = boughtAt <= soldAt;

                rows.Add(new CsvTradeRow(
                    symbol,
                    isLong ? "Long" : "Short",
                    qty,
                    EntryPrice: isLong ? buyPrice : sellPrice,
                    ExitPrice: isLong ? sellPrice : buyPrice,
                    EntryTime: isLong ? boughtAt : soldAt,
                    ExitTime: isLong ? soldAt : boughtAt,
                    GrossPnL: pnl,
                    Commission: 0m)); // Tradovate no reporta comisión por trade en este export
            }
            catch (Exception ex) when (ex is FormatException or IndexOutOfRangeException or OverflowException)
            {
                errors.Add(new CsvParseError(lineNumber, line, ex.Message));
            }
        }

        return new CsvParseResult(rows, errors);
    }

    private static decimal ParseDecimal(string raw)
    {
        var trimmed = raw.Trim();
        if (decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)) return value;
        throw new FormatException($"No se pudo interpretar '{raw}' como número.");
    }

    /// <summary>El pnl de Tradovate viene como "$21.25", "$(12.50)" (negativo) o "$1,033.00".</summary>
    private static decimal ParsePnl(string raw)
    {
        var trimmed = raw.Trim();
        const NumberStyles styles = NumberStyles.Currency;
        if (decimal.TryParse(trimmed, styles, CultureInfo.GetCultureInfo("en-US"), out var value)) return value;
        if (decimal.TryParse(trimmed.Replace("$", ""), styles, CultureInfo.InvariantCulture, out value)) return value;
        throw new FormatException($"No se pudo interpretar '{raw}' como P&L.");
    }

    private static DateTimeOffset ParseTimestamp(string raw)
    {
        var trimmed = raw.Trim();
        const DateTimeStyles styles = DateTimeStyles.AssumeLocal | DateTimeStyles.AdjustToUniversal;
        foreach (var format in TimestampFormats)
        {
            if (DateTimeOffset.TryParseExact(trimmed, format, CultureInfo.InvariantCulture, styles, out var exact))
            {
                return exact;
            }
        }
        if (DateTimeOffset.TryParse(trimmed, CultureInfo.InvariantCulture, styles, out var value)) return value;
        throw new FormatException($"No se pudo interpretar '{raw}' como fecha/hora.");
    }

    private static string Field(IReadOnlyList<string> fields, int index) =>
        index < fields.Count ? fields[index] : throw new IndexOutOfRangeException($"Fila con menos columnas de las esperadas (columna {index}).");

    private static Dictionary<string, int> BuildColumnIndex(IReadOnlyList<string> headers)
    {
        var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Count; i++)
        {
            index[headers[i].Trim()] = i;
        }
        return index;
    }

    private static int ResolveColumn(Dictionary<string, int> columnIndex, string[] aliases)
    {
        foreach (var alias in aliases)
        {
            if (columnIndex.TryGetValue(alias, out var idx)) return idx;
        }
        return -1;
    }
}
