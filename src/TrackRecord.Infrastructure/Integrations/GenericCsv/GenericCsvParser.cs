using System.Globalization;

namespace TrackRecord.Infrastructure.Integrations.GenericCsv;

public record GenericCsvRow(IReadOnlyList<string> Fields, int LineNumber, string RawLine);

/// <summary>
/// Lector de CSV genérico (RFC4180 simplificado, igual convención que NinjaTraderCsvParser): la
/// cabecera decide los nombres de columna, el resto son filas de datos sin asumir ningún formato
/// de broker concreto. La resolución de qué columna es qué campo la hace el llamador con el
/// mapeo elegido por el usuario (ver IGenericCsvImportService).
/// </summary>
public static class GenericCsvParser
{
    public static IReadOnlyList<string> ReadHeaders(TextReader reader)
    {
        var headerLine = reader.ReadLine();
        if (headerLine is null) return [];
        return SplitCsvLine(headerLine).Select(h => h.Trim()).ToList();
    }

    public static (Dictionary<string, int> ColumnIndex, IReadOnlyList<GenericCsvRow> Rows) ReadAll(TextReader reader)
    {
        var headerLine = reader.ReadLine();
        if (headerLine is null)
        {
            return (new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase), []);
        }

        var headers = SplitCsvLine(headerLine).Select(h => h.Trim()).ToList();
        var columnIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Count; i++)
        {
            columnIndex[headers[i]] = i;
        }

        var rows = new List<GenericCsvRow>();
        var lineNumber = 1;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            rows.Add(new GenericCsvRow(SplitCsvLine(line), lineNumber, line));
        }

        return (columnIndex, rows);
    }

    public static decimal ParseDecimal(string raw)
    {
        var trimmed = raw.Trim().Trim('"');
        if (decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)) return value;
        if (decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.CurrentCulture, out value)) return value;
        throw new FormatException($"No se pudo interpretar '{raw}' como número.");
    }

    public static DateTimeOffset ParseDate(string raw)
    {
        var trimmed = raw.Trim().Trim('"');
        if (DateTimeOffset.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var value)) return value;
        if (DateTimeOffset.TryParse(trimmed, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out value)) return value;
        throw new FormatException($"No se pudo interpretar '{raw}' como fecha/hora.");
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
