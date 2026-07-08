using System.Globalization;

namespace TrackRecord.Infrastructure.Integrations.NinjaTrader;

/// <summary>Una fila ya parseada del export "Trade Performance" de NinjaTrader 8 (un round-turn por fila).</summary>
public record CsvTradeRow(
    string Symbol,
    string MarketPosition, // "Long" | "Short"
    int Quantity,
    decimal EntryPrice,
    decimal ExitPrice,
    DateTimeOffset EntryTime,
    DateTimeOffset ExitTime,
    decimal GrossPnL,
    decimal Commission);

public record CsvParseError(int LineNumber, string RawLine, string Reason);

public record CsvParseResult(IReadOnlyList<CsvTradeRow> Rows, IReadOnlyList<CsvParseError> Errors);

/// <summary>
/// Parsea el CSV exportado desde NinjaTrader 8 (Control Center → Trade Performance → grid →
/// Export), que reporta round-turns YA agregados (no fills sueltos) — ver
/// GUIA_IMPLEMENTACION.md §6, Opción B.
///
/// NOTA: los nombres de columna y el formato exacto dependen de qué columnas tenga visibles el
/// grid en NT8 y de la cultura del sistema operativo del usuario (decimales/fechas). Este parser
/// busca las columnas por nombre (no por posición) entre una lista de alias tolerantes, y hace un
/// segundo intento con InvariantCulture si el parseo con la cultura indicada falla — pero debe
/// validarse contra un export real antes de confiar en él en producción (Apéndice A de la guía).
/// </summary>
public class NinjaTraderCsvParser
{
    private static readonly string[] InstrumentHeaders = ["Instrument", "Symbol"];
    private static readonly string[] MarketPosHeaders = ["Market pos.", "Market Position", "Direction", "Side"];
    private static readonly string[] QtyHeaders = ["Qty", "Quantity"];
    private static readonly string[] EntryPriceHeaders = ["Entry price", "Entry Price"];
    private static readonly string[] ExitPriceHeaders = ["Exit price", "Exit Price"];
    private static readonly string[] EntryTimeHeaders = ["Entry time", "Entry Time"];
    private static readonly string[] ExitTimeHeaders = ["Exit time", "Exit Time"];
    private static readonly string[] ProfitHeaders = ["Profit", "P/L", "Net Profit"];
    private static readonly string[] CommissionHeaders = ["Commission"];

    public CsvParseResult Parse(TextReader reader, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;

        var headerLine = reader.ReadLine();
        if (headerLine is null)
        {
            return new CsvParseResult([], [new CsvParseError(1, "", "El archivo está vacío.")]);
        }

        var headers = SplitCsvLine(headerLine);
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

            var fields = SplitCsvLine(line);
            try
            {
                var symbol = Field(fields, instrumentCol).Trim();
                var marketPos = NormalizeMarketPosition(Field(fields, marketPosCol).Trim());
                var qty = int.Parse(Field(fields, qtyCol).Trim(), NumberStyles.Integer, culture);
                var entryPrice = ParseDecimal(Field(fields, entryPriceCol), culture);
                var exitPrice = ParseDecimal(Field(fields, exitPriceCol), culture);
                var entryTime = ParseDate(Field(fields, entryTimeCol), culture);
                var exitTime = ParseDate(Field(fields, exitTimeCol), culture);
                var commission = commissionCol >= 0 ? ParseDecimal(Field(fields, commissionCol), culture) : 0m;
                var profit = profitCol >= 0 ? ParseDecimal(Field(fields, profitCol), culture) : ComputeFallbackGrossPnl(marketPos, entryPrice, exitPrice, qty);

                // Si "Profit" viene ya neto de comisión (convención habitual de NT8), el bruto es
                // Profit + Commission — así Trade.NetPnL (GrossPnL - Commissions) reproduce el
                // Profit original que reportó NT8.
                var grossPnL = profitCol >= 0 ? profit + commission : profit;

                rows.Add(new CsvTradeRow(symbol, marketPos, qty, entryPrice, exitPrice, entryTime, exitTime, grossPnL, commission));
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

    /// <summary>Split RFC4180 simplificado: soporta campos entre comillas con comas/comillas escapadas.</summary>
    private static List<string> SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }
}
