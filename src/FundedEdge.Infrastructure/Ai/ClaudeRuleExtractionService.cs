using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using FundedEdge.Application.Abstractions;
using FundedEdge.Application.Ai;

namespace FundedEdge.Infrastructure.Ai;

/// <summary>
/// Extrae programas de evaluación estructurados desde el reglamento (Markdown/HTML) de una firma
/// usando Claude. Reutiliza el <see cref="AnthropicClient"/> ya configurado en la app. Emite el
/// mismo esquema JSON que valida <c>ManualExternalFirmDataProvider</c>, de modo que el pipeline
/// automatizado y la importación manual comparten validación (DRY). Usa Haiku por coste: la tarea
/// es de extracción/mapeo, no de razonamiento profundo.
/// </summary>
public sealed class ClaudeRuleExtractionService(AnthropicClient client, AiOptions options) : IRuleExtractionService
{
    private const string SystemPrompt =
        """
        Eres un extractor de datos. Recibes el reglamento (en Markdown o HTML) de una prop firm de
        futuros y devuelves EXCLUSIVAMENTE un array JSON con sus programas de evaluación por tamaño
        de cuenta. Nada de prosa, nada de explicaciones, nada de ```; solo el array JSON.

        Cada elemento del array tiene exactamente estos campos (omite los opcionales si el
        reglamento no los especifica; no inventes valores):
          - "Name" (string, obligatorio): nombre comercial del programa + tamaño, p.ej. "Apex 50K".
          - "AccountSize" (number, obligatorio): tamaño de la cuenta en USD.
          - "EvaluationCost" (number): cuota de la evaluación.
          - "ActivationCost" (number): cuota de activación al pasar a fondeada (0 si no la hay).
          - "ProfitTarget" (number, obligatorio): objetivo de beneficio de la evaluación.
          - "MaxDrawdown" (number, obligatorio): pérdida máxima permitida en evaluación.
          - "DrawdownType" (string): uno de "Static", "Trailing", "EndOfDay".
          - "DailyLossLimit" (number|null): límite de pérdida diaria (null si no hay).
          - "MinTradingDays" (integer|null): días mínimos de trading.
          - "ConsistencyMaxDayFraction" (number|null): regla de consistencia como fracción 0-1 (30%→0.30).
          - "FundedMaxDrawdown" (number|null): drawdown en fase fondeada (null = igual que evaluación).
          - "FundedDrawdownType" (string|null): "Static"|"Trailing"|"EndOfDay" en fondeada.
          - "FundedDailyLossLimit" (number|null): límite diario en fondeada.
          - "FundedProfitTarget" (number|null): objetivo en fondeada (normalmente null).
          - "FundedMinTradingDays" (integer|null): días mínimos antes del primer payout.
          - "PayoutSplitTraderPct" (number): fracción para el trader 0-1 (90%→0.90; por defecto 1.0).
          - "PayoutMaxProfitPct" (number|null): tope de retiro como fracción del profit (50%→0.50).
          - "PayoutMinDaysBetween" (integer|null): días mínimos entre payouts.

        Si el reglamento describe varias vías (p.ej. EOD e Intraday) con parámetros distintos, emite
        un elemento por combinación tamaño×vía y refléjalo en "Name" (p.ej. "Apex 50K Intraday").
        """;

    public bool IsConfigured => options.IsApiKeyConfigured;

    public async Task<string> ExtractProgramsJsonAsync(string firmName, string rulesContent, CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException(
                "No hay una API key de Anthropic configurada. Define ANTHROPIC_API_KEY (o \"Ai:ApiKey\"). Ver README.md.");
        if (string.IsNullOrWhiteSpace(rulesContent))
            throw new ArgumentException("El contenido del reglamento está vacío.", nameof(rulesContent));

        var userContent = $"Firma: {firmName}\n\nReglamento:\n{rulesContent}";
        var parameters = new MessageCreateParams
        {
            Model = Model.ClaudeHaiku4_5,
            MaxTokens = 8192,
            System = new List<TextBlockParam>
            {
                new() { Text = SystemPrompt, CacheControl = new CacheControlEphemeral() },
            },
            Messages = [new() { Role = Role.User, Content = userContent }],
        };

        Message response;
        try
        {
            response = await client.Messages.Create(parameters);
        }
        catch (AnthropicApiException ex)
        {
            throw new InvalidOperationException($"Error llamando a la API de Claude: {ex.Message}", ex);
        }

        var text = string.Concat(response.Content
            .Select(block => block.TryPickText(out TextBlock? textBlock) ? textBlock!.Text : null)
            .Where(s => s is not null));

        return StripCodeFences(text).Trim();
    }

    /// <summary>Quita vallas ```json … ``` si el modelo las añade pese a las instrucciones.</summary>
    private static string StripCodeFences(string s)
    {
        s = s.Trim();
        if (!s.StartsWith("```")) return s;
        var firstNewline = s.IndexOf('\n');
        if (firstNewline >= 0) s = s[(firstNewline + 1)..];
        if (s.EndsWith("```")) s = s[..^3];
        return s;
    }
}
