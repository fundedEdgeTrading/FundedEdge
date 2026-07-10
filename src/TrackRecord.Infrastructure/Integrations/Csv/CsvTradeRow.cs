namespace TrackRecord.Infrastructure.Integrations.Csv;

/// <summary>
/// Fila normalizada de un export de round-turns, independiente de la plataforma de origen
/// (Tradovate o NinjaTrader 8). Es el modelo unificado sobre el que trabaja
/// <see cref="CsvTradeImportService"/>: cada parser específico traduce las columnas de su
/// plataforma a este mismo esquema, de forma que aguas abajo todos los cálculos (KPIs, curva de
/// equity, MAE/MFE, cumplimiento de reglas…) funcionan igual venga de donde venga el archivo.
///
/// Diferencias conocidas entre plataformas:
///  - NinjaTrader 8 puede aportar Commission y MAE/MFE si esas columnas están visibles en el grid.
///  - El export de rendimiento de Tradovate no incluye comisiones ni excursiones — esos campos
///    quedan a 0/null y pueden completarse a mano después si interesa.
/// </summary>
public record CsvTradeRow(
    string Symbol,
    string MarketPosition, // "Long" | "Short"
    int Quantity,
    decimal EntryPrice,
    decimal ExitPrice,
    DateTimeOffset EntryTime,
    DateTimeOffset ExitTime,
    decimal GrossPnL,
    decimal Commission,
    decimal? MaxAdverseExcursion = null,
    decimal? MaxFavorableExcursion = null);

public record CsvParseError(int LineNumber, string RawLine, string Reason);

public record CsvParseResult(IReadOnlyList<CsvTradeRow> Rows, IReadOnlyList<CsvParseError> Errors);

/// <summary>Split RFC4180 simplificado compartido por los parsers: detecta automáticamente el delimitador (coma o punto y coma).</summary>
internal static class CsvLineSplitter
{
    private static char _detectedDelimiter = ',';

    public static List<string> Split(string line)
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
            else if (c == _detectedDelimiter)
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

    public static void DetectDelimiter(string headerLine)
    {
        var commaCount = 0;
        var semiCount = 0;
        var inQuotes = false;

        foreach (var c in headerLine)
        {
            if (c == '"') inQuotes = !inQuotes;
            else if (!inQuotes)
            {
                if (c == ',') commaCount++;
                else if (c == ';') semiCount++;
            }
        }

        _detectedDelimiter = semiCount > commaCount ? ';' : ',';
    }
}
