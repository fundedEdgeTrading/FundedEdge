using System.Globalization;

namespace TrackRecord.Infrastructure.Integrations.Csv;

/// <summary>
/// Parsea el CSV exportado desde NinjaTrader 8 (Control Center → Trade Performance → pestaña
/// Trades → clic derecho sobre el grid → Export), que reporta round-turns YA agregados (no fills
/// sueltos).
///
/// Columnas típicas del export: Trade number, Instrument, Account, Strategy, Market pos., Qty,
/// Entry price, Exit price, Entry time, Exit time, Entry name, Exit name, Profit,
/// Cum. net profit, Commission, MAE, MFE, ETD, Bars. Solo un subconjunto es obligatorio.
///
/// NOTA: los nombres de columna y el formato exacto dependen de qué columnas tenga visibles el
/// grid en NT8 y de la cultura del sistema operativo del usuario (decimales/fechas). Este parser
/// busca las columnas por nombre (no por posición) entre una lista de alias tolerantes, y hace un
/// segundo intento con InvariantCulture si el parseo con la cultura indicada falla.
/// </summary>
public class NinjaTraderCsvParser
{
    private static readonly string[] InstrumentHeaders = ["Instrument", "Symbol", "Instrumento"];
    private static readonly string[] MarketPosHeaders = ["Market pos.", "Market Position", "Direction", "Side", "Mercado pos."];
    private static readonly string[] QtyHeaders = ["Qty", "Quantity", "Cant."];
    private static readonly string[] EntryPriceHeaders = ["Entry price", "Entry Price", "Precio de entrada"];
    private static readonly string[] ExitPriceHeaders = ["Exit price", "Exit Price", "Precio de salida"];
    private static readonly string[] EntryTimeHeaders = ["Entry time", "Entry Time", "Tiempo de entrada"];
    private static readonly string[] ExitTimeHeaders = ["Exit time", "Exit Time", "Tiempo de salida"];
    private static readonly string[] ProfitHeaders = ["Profit", "P/L", "Net Profit", "Ganancias"];
    private static readonly string[] CommissionHeaders = ["Commission", "Comisión"];
    private static readonly string[] MaeHeaders = ["MAE", "Max. adverse excursion"];
    private static readonly string[] MfeHeaders = ["MFE", "Max. favorable excursion"];

    /// <summary>Heurística de detección: cabeceras características del export de NT8.</summary>
    public static bool LooksLikeHeader(IReadOnlyList<string> headers)
    {
        bool Has(string[] aliases)
        {
            return headers.Any(h =>
                aliases.Any(alias => string.Equals(h.Trim(), alias, StringComparison.OrdinalIgnoreCase)));
        }

        return Has(EntryPriceHeaders) && Has(EntryTimeHeaders) && Has(MarketPosHeaders);
    }

    public CsvParseResult Parse(TextReader reader, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;

        var headerLine = reader.ReadLine();
        if (headerLine is null)
        {
            return new CsvParseResult([], [new CsvParseError(1, "", "El archivo está vacío.")]);
        }

        CsvLineSplitter.DetectDelimiter(headerLine);
        var headers = CsvLineSplitter.Split(headerLine);
        var columnIndex = BuildColumnIndex(headers);

        var missing = new List<string>();
        int Require(string[] aliases, string label)
        {
            var idx = ResolveColumn(columnIndex, aliases);
            if (idx < 0) missing.Add(label);
            return idx;
        }

        var instrumentCol = Require(InstrumentHeaders, "Instrument");
        var marketPosCol = Require(MarketPosHeaders, "Market pos.");
        var qtyCol = Require(QtyHeaders, "Qty");
        var entryPriceCol = Require(EntryPriceHeaders, "Entry price");
        var exitPriceCol = Require(ExitPriceHeaders, "Exit price");
        var entryTimeCol = Require(EntryTimeHeaders, "Entry time");
        var exitTimeCol = Require(ExitTimeHeaders, "Exit time");
        var profitCol = ResolveColumn(columnIndex, ProfitHeaders); // opcional
        var commissionCol = ResolveColumn(columnIndex, CommissionHeaders); // opcional
        var maeCol = ResolveColumn(columnIndex, MaeHeaders); // opcional
        var mfeCol = ResolveColumn(columnIndex, MfeHeaders); // opcional

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
                var symbol = Field(fields, instrumentCol).Trim();
                var marketPos = NormalizeMarketPosition(Field(fields, marketPosCol).Trim());
                var qty = int.Parse(Field(fields, qtyCol).Trim(), NumberStyles.Integer, culture);
                var entryPrice = ParseDecimal(Field(fields, entryPriceCol), culture);
                var exitPrice = ParseDecimal(Field(fields, exitPriceCol), culture);
                var entryTime = ParseDate(Field(fields, entryTimeCol), culture);
                var exitTime = ParseDate(Field(fields, exitTimeCol), culture);
                var commission = commissionCol >= 0 ? ParseCurrency(Field(fields, commissionCol), culture) : 0m;
                var profit = profitCol >= 0 ? ParseCurrency(Field(fields, profitCol), culture) : ComputeFallbackGrossPnl(marketPos, entryPrice, exitPrice, qty);
                var mae = maeCol >= 0 ? TryParseOptionalCurrency(Field(fields, maeCol), culture) : null;
                var mfe = mfeCol >= 0 ? TryParseOptionalCurrency(Field(fields, mfeCol), culture) : null;

                // Si "Profit" viene ya neto de comisión (convención habitual de NT8), el bruto es
                // Profit + Commission — así Trade.NetPnL (GrossPnL - Commissions) reproduce el
                // Profit original que reportó NT8.
                var grossPnL = profitCol >= 0 ? profit + commission : profit;

                // MAE se guarda como magnitud positiva de la pérdida flotante máxima.
                rows.Add(new CsvTradeRow(symbol, marketPos, qty, entryPrice, exitPrice, entryTime, exitTime, grossPnL, commission,
                    mae is { } m ? Math.Abs(m) : null, mfe));
            }
            catch (Exception ex) when (ex is FormatException or IndexOutOfRangeException or OverflowException)
            {
                errors.Add(new CsvParseError(lineNumber, line, ex.Message));
            }
        }

        return new CsvParseResult(rows, errors);
    }

    private static decimal ComputeFallbackGrossPnl(string marketPosition, decimal entryPrice, decimal exitPrice, int quantity)
    {
        // Sin columna Profit, se calcula en puntos de precio en bruto (sin tick value del
        // instrumento) — mejor que nada para una reconciliación rápida, pero se recomienda
        // exportar con la columna "Profit" visible para un cálculo exacto.
        var sign = marketPosition == "Long" ? 1 : -1;
        return (exitPrice - entryPrice) * quantity * sign;
    }

    private static string NormalizeMarketPosition(string raw) => raw switch
    {
        "Long" or "Buy" or "L" => "Long",
        "Short" or "Sell" or "S" => "Short",
        _ => throw new FormatException($"Market pos. desconocido: '{raw}'."),
    };

    private static decimal ParseDecimal(string raw, CultureInfo culture)
    {
        var trimmed = raw.Trim();
        if (decimal.TryParse(trimmed, NumberStyles.Number, culture, out var value)) return value;
        if (decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out value)) return value;
        throw new FormatException($"No se pudo interpretar '{raw}' como número.");
    }

    /// <summary>Como ParseDecimal pero tolera símbolo de divisa y paréntesis para negativos ("($12.50)"), formatos que NT8 usa según configuración.</summary>
    private static decimal ParseCurrency(string raw, CultureInfo culture)
    {
        var trimmed = raw.Trim();
        const NumberStyles styles = NumberStyles.Currency;
        if (decimal.TryParse(trimmed, styles, culture, out var value)) return value;
        if (decimal.TryParse(trimmed, styles, CultureInfo.InvariantCulture, out value)) return value;
        if (decimal.TryParse(trimmed.Replace("$", ""), styles, CultureInfo.InvariantCulture, out value)) return value;
        throw new FormatException($"No se pudo interpretar '{raw}' como importe.");
    }

    private static decimal? TryParseOptionalCurrency(string raw, CultureInfo culture)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        try { return ParseCurrency(raw, culture); }
        catch (FormatException) { return null; } // MAE/MFE son un extra: una celda rara no debe tumbar la fila
    }

    private static DateTimeOffset ParseDate(string raw, CultureInfo culture)
    {
        var trimmed = raw.Trim();
        if (DateTimeOffset.TryParse(trimmed, culture, DateTimeStyles.AssumeLocal, out var value)) return value;
        if (DateTimeOffset.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out value)) return value;
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
