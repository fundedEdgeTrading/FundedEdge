using System.Net;
using System.Text.RegularExpressions;

namespace TrackRecord.Infrastructure.RuleMonitor;

/// <summary>
/// Reduce el HTML de una página a su texto visible para que el hash de cambio no se dispare con
/// rediseños cosméticos (scripts, estilos, atributos, tokens CSRF) y para que la extracción LLM
/// reciba solo contenido, no markup (poda de tokens, INVESTIGACION_AUTOMATIZACION_REGLAS.md §3.3).
/// </summary>
public static partial class HtmlContentNormalizer
{
    [GeneratedRegex(@"<(script|style|noscript|svg|head)[^>]*>.*?</\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex NonContentBlocks();

    [GeneratedRegex(@"<!--.*?-->", RegexOptions.Singleline)]
    private static partial Regex Comments();

    [GeneratedRegex(@"<(br|/p|/div|/li|/tr|/h[1-6])[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex BlockBreaks();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex AnyTag();

    [GeneratedRegex(@"[ \t]+")]
    private static partial Regex HorizontalWhitespace();

    [GeneratedRegex(@"\n{2,}")]
    private static partial Regex BlankLines();

    public static string Normalize(string html)
    {
        var text = NonContentBlocks().Replace(html, " ");
        text = Comments().Replace(text, " ");
        text = BlockBreaks().Replace(text, "\n"); // conserva la separación de párrafos/filas
        text = AnyTag().Replace(text, " ");
        text = WebUtility.HtmlDecode(text);
        text = HorizontalWhitespace().Replace(text, " ");
        text = string.Join('\n', text.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));
        return BlankLines().Replace(text, "\n").Trim();
    }
}
