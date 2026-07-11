using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TrackRecord.Application.Abstractions;
using TrackRecord.Application.Dtos;
using TrackRecord.Domain.Entities;
using TrackRecord.Infrastructure.Ai;
using TrackRecord.Infrastructure.Persistence;

namespace TrackRecord.Infrastructure.RuleMonitor;

/// <summary>
/// Extracción schema-driven con Claude (INVESTIGACION_AUTOMATIZACION_REGLAS.md §3.3 y §5): el
/// texto normalizado de la página se envía con un JSON schema estricto que replica los campos de
/// <see cref="EvaluationProgram"/> más citas literales por campo y confianza. Lo extraído se
/// valida (rangos plausibles), se compara con el catálogo activo de la firma y solo las
/// diferencias acaban como <see cref="ProposedProgramChange"/> pendientes de revisión.
/// </summary>
public class ClaudeRuleExtractionService(
    AnthropicClient client,
    AiOptions options,
    IDbContextFactory<TrackRecordDbContext> dbFactory,
    ILogger<ClaudeRuleExtractionService> logger) : IRuleExtractionService
{
    /// <summary>Tope de texto de página enviado al modelo (control de tokens; las páginas de reglas reales son mucho más cortas).</summary>
    private const int MaxPageTextChars = 80_000;

    private const string SystemPrompt =
        """
        Eres un extractor de datos de prop firms de futuros. Recibirás el texto visible de una
        página oficial de una firma (pricing, FAQ o reglas) y devolverás, en el formato JSON
        exigido, los programas de evaluación que la página describe con sus reglas exactas.

        Reglas estrictas:
        1. Extrae SOLO lo que la página dice de forma explícita. Si un campo no aparece, déjalo
           en null. No completes con conocimiento previo de la firma ni con valores típicos.
        2. Importes en USD absolutos. Si la página expresa un valor como porcentaje del tamaño de
           cuenta (p.ej. "profit target 6%"), conviértelo a USD usando el tamaño de la cuenta.
        3. Los campos consistencyMaxDayFraction, payoutSplitTraderPct y payoutMaxProfitPct son
           fracciones 0-1 (p.ej. "regla del 30 %" → 0.30; "el trader cobra el 90 %" → 0.90).
        4. drawdownType: "Trailing" (sigue el pico, intradía), "EndOfDay" (se actualiza al cierre)
           o "Static" (fijo desde el balance inicial). Los campos funded* solo si la página
           distingue reglas de cuenta fondeada distintas de las de evaluación.
        5. Por cada campo extraído añade en "quotes" una cita LITERAL y breve de la página que lo
           sustente (clave = nombre del campo). Un campo sin cita no debería extraerse.
        6. "confidence" 0-1 por programa: baja si la página es ambigua o el dato está lejos del
           nombre del programa.
        7. Ignora contenido promocional, testimonios y programas de otras firmas mencionados en
           comparativas. Un programa por tamaño de cuenta (p.ej. 50K y 100K son dos programas).
        """;

    public bool IsConfigured => options.IsApiKeyConfigured;

    public async Task<int> ProposeFromContentAsync(Guid ruleSourceId, string pageText, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "No hay una API key de Anthropic configurada. Define la variable de entorno " +
                "ANTHROPIC_API_KEY (o \"Ai:ApiKey\" en appsettings.Development.json / user-secrets) " +
                "y reinicia la aplicación. Ver README.md.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var source = await db.RuleSources
            .AsNoTracking()
            .Include(s => s.PropFirm)
            .SingleOrDefaultAsync(s => s.Id == ruleSourceId, ct)
            ?? throw new KeyNotFoundException($"RuleSource {ruleSourceId} no encontrada.");

        var firmName = source.PropFirm?.Name ?? "(desconocida)";
        var activePrograms = await db.EvaluationPrograms
            .Where(p => p.PropFirmId == source.PropFirmId && p.IsActive)
            .ToListAsync(ct);

        var extracted = await ExtractAsync(firmName, activePrograms, pageText, ct);
        if (extracted.Count == 0)
        {
            logger.LogInformation("Extracción de {Url}: la página no contiene programas extraíbles.", source.Url);
            return 0;
        }

        var proposals = 0;
        foreach (var rules in extracted)
        {
            if (!IsPlausible(rules, out var reason))
            {
                logger.LogWarning("Extracción de {Url}: programa \"{Name}\" descartado ({Reason}).", source.Url, rules.Name, reason);
                continue;
            }

            var existing = MatchExisting(activePrograms, rules);
            var diffs = ProgramDiffCalculator.ComputeDiffs(rules, existing);
            if (existing is not null && diffs.Count == 0) continue; // catálogo ya al día

            // Un programa nuevo necesita las reglas mínimas para poder crearse al aprobar.
            if (existing is null &&
                (rules.EvaluationCost is null || rules.ProfitTarget is null || rules.MaxDrawdown is null || rules.DrawdownType is null))
            {
                logger.LogWarning("Extracción de {Url}: programa nuevo \"{Name}\" incompleto (faltan coste/target/drawdown), descartado.", source.Url, rules.Name);
                continue;
            }

            // Una propuesta pendiente por firma+programa: la extracción más reciente la refresca
            // en vez de acumular duplicados en la cola de revisión.
            var proposal = await db.ProposedProgramChanges.SingleOrDefaultAsync(
                p => p.PropFirmId == source.PropFirmId
                     && p.ProgramName == rules.Name
                     && p.Status == Domain.Enums.ProposalStatus.Pending, ct);

            if (proposal is null)
            {
                proposal = new ProposedProgramChange { PropFirmId = source.PropFirmId, ProgramName = rules.Name };
                db.ProposedProgramChanges.Add(proposal);
            }

            proposal.ExistingProgramId = existing?.Id;
            proposal.SourceUrl = source.Url;
            proposal.PayloadJson = JsonSerializer.Serialize(rules, RuleExtractionJson.Options);
            proposal.CreatedAt = DateTimeOffset.UtcNow;
            proposals++;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Extracción de {Url}: {Count} propuesta(s) pendientes de revisión.", source.Url, proposals);
        return proposals;
    }

    private async Task<IReadOnlyList<ExtractedProgramRules>> ExtractAsync(
        string firmName, List<EvaluationProgram> activePrograms, string pageText, CancellationToken ct)
    {
        if (pageText.Length > MaxPageTextChars)
        {
            pageText = pageText[..MaxPageTextChars];
        }

        // El catálogo vigente ancla el matching por nombre (el modelo reutiliza el nombre del
        // programa existente cuando la página se refiere claramente al mismo).
        var knownPrograms = activePrograms.Count == 0
            ? "(ninguno todavía)"
            : string.Join("\n", activePrograms.Select(p => $"- {p.Name} (cuenta de {p.AccountSize:0} USD)"));

        var userContent =
            $"""
            Firma: {firmName}

            Programas ya registrados en el catálogo (reutiliza estos nombres si la página habla del mismo programa):
            {knownPrograms}

            Texto de la página oficial:
            ---
            {pageText}
            ---
            """;

        var parameters = new MessageCreateParams
        {
            Model = Model.ClaudeOpus4_8,
            MaxTokens = 16000,
            Thinking = new ThinkingConfigAdaptive(),
            System = new List<TextBlockParam>
            {
                new() { Text = SystemPrompt, CacheControl = new CacheControlEphemeral() },
            },
            OutputConfig = new OutputConfig { Format = new JsonOutputFormat { Schema = BuildSchema() } },
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

        var json = string.Concat(response.Content
            .Select(block => block.TryPickText(out TextBlock? textBlock) ? textBlock!.Text : null)
            .Where(s => s is not null));

        var envelope = JsonSerializer.Deserialize<ExtractionEnvelope>(json, RuleExtractionJson.Options);
        return envelope?.Programs ?? [];
    }

    /// <summary>Match contra el catálogo activo: por nombre y, si no, por tamaño de cuenta único.</summary>
    private static EvaluationProgram? MatchExisting(List<EvaluationProgram> active, ExtractedProgramRules rules)
    {
        var byName = active.FirstOrDefault(p => string.Equals(p.Name.Trim(), rules.Name.Trim(), StringComparison.OrdinalIgnoreCase));
        if (byName is not null) return byName;

        var bySize = active.Where(p => p.AccountSize == rules.AccountSize).ToList();
        return bySize.Count == 1 ? bySize[0] : null;
    }

    /// <summary>
    /// Rangos plausibles (INVESTIGACION_AUTOMATIZACION_REGLAS.md §5.3): target y drawdown
    /// proporcionales al tamaño de cuenta, fracciones en (0,1], costes no negativos. Un valor
    /// fuera de rango delata una alucinación o un error de unidad y descarta el programa entero.
    /// </summary>
    private static bool IsPlausible(ExtractedProgramRules p, out string? reason)
    {
        reason = null;
        if (string.IsNullOrWhiteSpace(p.Name)) { reason = "sin nombre"; return false; }
        if (p.AccountSize is < 1_000 or > 5_000_000) { reason = $"accountSize {p.AccountSize} fuera de rango"; return false; }
        if (p.Confidence is < 0 or > 1) { reason = $"confidence {p.Confidence} fuera de rango"; return false; }

        var maxProportional = p.AccountSize * 0.25m;
        foreach (var (name, value) in new (string, decimal?)[]
                 {
                     ("profitTarget", p.ProfitTarget), ("maxDrawdown", p.MaxDrawdown),
                     ("dailyLossLimit", p.DailyLossLimit), ("fundedMaxDrawdown", p.FundedMaxDrawdown),
                     ("fundedDailyLossLimit", p.FundedDailyLossLimit), ("fundedProfitTarget", p.FundedProfitTarget),
                 })
        {
            if (value is not null && (value <= 0 || value > maxProportional))
            {
                reason = $"{name} {value} no es proporcional a la cuenta de {p.AccountSize}";
                return false;
            }
        }

        foreach (var (name, value) in new (string, decimal?)[]
                 {
                     ("consistencyMaxDayFraction", p.ConsistencyMaxDayFraction),
                     ("payoutSplitTraderPct", p.PayoutSplitTraderPct),
                     ("payoutMaxProfitPct", p.PayoutMaxProfitPct),
                 })
        {
            if (value is <= 0 or > 1) { reason = $"{name} {value} no es una fracción 0-1"; return false; }
        }

        if (p.EvaluationCost is < 0 or > 10_000) { reason = $"evaluationCost {p.EvaluationCost} fuera de rango"; return false; }
        if (p.ActivationCost is < 0 or > 10_000) { reason = $"activationCost {p.ActivationCost} fuera de rango"; return false; }
        if (p.MinTradingDays is < 0 or > 90) { reason = $"minTradingDays {p.MinTradingDays} fuera de rango"; return false; }
        if (p.FundedMinTradingDays is < 0 or > 90) { reason = $"fundedMinTradingDays {p.FundedMinTradingDays} fuera de rango"; return false; }
        if (p.PayoutMinDaysBetween is < 0 or > 90) { reason = $"payoutMinDaysBetween {p.PayoutMinDaysBetween} fuera de rango"; return false; }

        return true;
    }

    private sealed record ExtractionEnvelope(List<ExtractedProgramRules> Programs);

    /// <summary>Schema estricto de la salida: replica ExtractedProgramRules (null = "no está en la página").</summary>
    private static Dictionary<string, JsonElement> BuildSchema()
    {
        static object NullableNumber() => new { type = new[] { "number", "null" } };
        static object NullableInteger() => new { type = new[] { "integer", "null" } };
        static object NullableDrawdownType() => new
        {
            anyOf = new object[]
            {
                new { type = "string", @enum = new[] { "Trailing", "EndOfDay", "Static" } },
                new { type = "null" },
            },
        };

        var programSchema = new
        {
            type = "object",
            properties = new Dictionary<string, object>
            {
                ["name"] = new { type = "string" },
                ["accountSize"] = new { type = "number" },
                ["evaluationCost"] = NullableNumber(),
                ["activationCost"] = NullableNumber(),
                ["profitTarget"] = NullableNumber(),
                ["maxDrawdown"] = NullableNumber(),
                ["drawdownType"] = NullableDrawdownType(),
                ["dailyLossLimit"] = NullableNumber(),
                ["minTradingDays"] = NullableInteger(),
                ["consistencyMaxDayFraction"] = NullableNumber(),
                ["fundedMaxDrawdown"] = NullableNumber(),
                ["fundedDrawdownType"] = NullableDrawdownType(),
                ["fundedDailyLossLimit"] = NullableNumber(),
                ["fundedProfitTarget"] = NullableNumber(),
                ["fundedMinTradingDays"] = NullableInteger(),
                ["payoutSplitTraderPct"] = NullableNumber(),
                ["payoutMaxProfitPct"] = NullableNumber(),
                ["payoutMinDaysBetween"] = NullableInteger(),
                ["confidence"] = new { type = "number" },
                ["quotes"] = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new Dictionary<string, object>
                        {
                            ["field"] = new { type = "string" },
                            ["quote"] = new { type = "string" },
                        },
                        required = new[] { "field", "quote" },
                        additionalProperties = false,
                    },
                },
            },
            required = new[]
            {
                "name", "accountSize", "evaluationCost", "activationCost", "profitTarget", "maxDrawdown",
                "drawdownType", "dailyLossLimit", "minTradingDays", "consistencyMaxDayFraction",
                "fundedMaxDrawdown", "fundedDrawdownType", "fundedDailyLossLimit", "fundedProfitTarget",
                "fundedMinTradingDays", "payoutSplitTraderPct", "payoutMaxProfitPct", "payoutMinDaysBetween",
                "confidence", "quotes",
            },
            additionalProperties = false,
        };

        return new Dictionary<string, JsonElement>
        {
            ["type"] = JsonSerializer.SerializeToElement("object"),
            ["properties"] = JsonSerializer.SerializeToElement(new Dictionary<string, object>
            {
                ["programs"] = new { type = "array", items = programSchema },
            }),
            ["required"] = JsonSerializer.SerializeToElement(new[] { "programs" }),
            ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
        };
    }
}
