using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TrackRecord.Application.Abstractions;
using TrackRecord.Application.Ai;
using TrackRecord.Application.Dtos;
using TrackRecord.Domain.Entities;
using TrackRecord.Domain.Enums;
using TrackRecord.Infrastructure.Persistence;

namespace TrackRecord.Infrastructure.Ai;

/// <summary>
/// Análisis de la operativa y del negocio de fondeo con Claude, a partir de los KPIs agregados
/// (nunca trades individuales) calculados por IKpiService. Ver GUIA_IMPLEMENTACION.md §9.
/// </summary>
public class ClaudeTradingAnalystService(
    AnthropicClient client,
    AiOptions options,
    IKpiService kpiService,
    IRiskAnalysisService riskService,
    ICurrentUserAccessor currentUser,
    IPlanService planService,
    IDbContextFactory<TrackRecordDbContext> dbFactory) : ITradingAnalystService
{
    private const string SystemPrompt =
        """
        Eres un analista cuantitativo especializado en trading de futuros con cuentas de fondeo
        (prop firms). Recibirás estadísticas agregadas -nunca trades individuales- de la operativa
        y del negocio de fondeo de un trader: KPIs de negocio (evaluaciones compradas, pass rate,
        coste por cuenta fondeada, payouts, ROI) y KPIs de trading (win rate, profit factor,
        expectancy, R-múltiplo, drawdown, rachas de pérdidas/ganancias). Si hay muestra
        suficiente, el bloque "riesgo" incluye además la esperanza matemática por evaluación con
        su intervalo de confianza al 95 % (bootstrap), la fracción de Kelly y una simulación Monte
        Carlo de ruina del bankroll bajo un escenario estándar documentado en "supuestos" — úsalo
        como ancla cuantitativa de la viabilidad, citando el intervalo y no solo el punto.

        Tu trabajo:
        1. Diagnosticar fortalezas y fugas de dinero concretas, citando siempre la métrica exacta
           que las sustenta.
        2. Evaluar la viabilidad estadística del negocio de cuentas de fondeo: ¿la esperanza
           matemática del funnel (pass rate, coste de evaluación, payout medio) es positiva?
           ¿el riesgo de la operativa (drawdown, rachas de pérdidas) es compatible con sobrevivir
           a las reglas de la prop firm?
        3. Proponer un plan de acción priorizado (máximo 5 puntos), con experimentos medibles.

        Sé directo y escéptico: si la muestra de datos es pequeña (menos de ~20 trades cerrados o
        menos de ~10 evaluaciones terminadas), dilo explícitamente y evita conclusiones que los
        datos no soporten. Responde siempre en español y en Markdown.
        """;

    public bool IsConfigured => options.IsApiKeyConfigured;

    public async Task<AiReportDto> GenerateAnalysisReportAsync(CancellationToken ct = default)
    {
        await EnsureAllowedAsync(AiReportKind.Analysis, ct);
        var payload = await BuildStatsPayloadAsync(ct);
        return await CallClaudeAndPersistAsync(AiReportKind.Analysis, null, payload, ct);
    }

    public async Task<AiReportDto> AskQuestionAsync(string question, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("La pregunta no puede estar vacía.", nameof(question));
        }

        await EnsureAllowedAsync(AiReportKind.AdHocQuestion, ct);
        var payload = await BuildStatsPayloadAsync(ct);
        var userContent = $"{payload}\n\nPregunta del usuario: {question}";
        return await CallClaudeAndPersistAsync(AiReportKind.AdHocQuestion, question, userContent, ct);
    }

    /// <summary>Lanza si el usuario ha agotado el cupo de IA de su plan (informes completos o preguntas ad-hoc).</summary>
    private async Task EnsureAllowedAsync(AiReportKind kind, CancellationToken ct)
    {
        var allowance = await planService.GetAiAllowanceAsync(ct);

        if (kind == AiReportKind.Analysis && !allowance.CanGenerateReport)
        {
            throw new InvalidOperationException(
                $"Has alcanzado el límite de informes completos de tu plan ({allowance.ReportsUsed}/{allowance.ReportsLimit}). " +
                $"Se renueva el {allowance.WindowResetsAt:d MMM yyyy}. Mejora tu plan en /plan para generar más.");
        }

        if (kind == AiReportKind.AdHocQuestion && !allowance.CanAskQuestion)
        {
            throw new InvalidOperationException(
                $"Has alcanzado tu límite de preguntas de este mes ({allowance.QuestionsUsed}/{allowance.QuestionsLimit}). " +
                "Mejora tu plan en /plan para preguntar más.");
        }
    }

    public async Task<IReadOnlyList<AiReportDto>> GetHistoryAsync(int take = 20, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.AiReports
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(take)
            .Select(r => new AiReportDto(r.Id, r.Kind, r.Question, r.Content, r.CreatedAt, r.Model, r.InputTokens, r.OutputTokens))
            .ToListAsync(ct);
    }

    private async Task<string> BuildStatsPayloadAsync(CancellationToken ct)
    {
        var business = await kpiService.GetBusinessKpisAsync(ct: ct);
        var trading = await kpiService.GetTradingKpisAsync(ct: ct);
        var riesgo = await BuildRiskSectionAsync(ct);

        return JsonSerializer.Serialize(
            new { negocio = business, trading, riesgo },
            new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Métricas del módulo de riesgo (§10) para el prompt: EV con IC, Kelly y P(ruina) de un
    /// escenario estándar documentado. Si no hay muestra suficiente devuelve null y el informe
    /// sigue funcionando solo con los KPIs.
    /// </summary>
    private async Task<object?> BuildRiskSectionAsync(CancellationToken ct)
    {
        var defaults = await riskService.GetDefaultsAsync(ct);
        if (defaults.Ev is null)
        {
            return null;
        }

        object? escenarioEstandar = null;
        if (defaults.PassRate is not null && defaults.AvgEvaluationCost is > 0)
        {
            // Escenario de referencia (no el bankroll real del usuario, que la app no conoce):
            // 10x el coste medio de evaluación, 2 compras/mes, 12 meses.
            var plan = await riskService.RunBankrollPlanAsync(new BankrollPlanRequest(
                Bankroll: defaults.AvgEvaluationCost.Value * 10,
                MonthlyEvaluationBudget: 2,
                Months: 12,
                Iterations: 5_000), ct);

            escenarioEstandar = new
            {
                supuestos = "bankroll = 10x coste medio de evaluación, 2 evaluaciones/mes, horizonte 12 meses",
                bankrollSimulado = plan.InputsUsed.Bankroll,
                probabilidadDeRuina = plan.Simulation.ProbabilityOfRuin,
                bankrollFinalMediana = plan.Simulation.MedianFinalBankroll,
                bankrollFinalP5 = plan.Simulation.P5FinalBankroll,
                mesesHastaBreakevenMediana = plan.Simulation.MedianMonthsToBreakeven,
                bankrollMinimoParaRuinaBajo5Pct = plan.MinimumBankrollFor5PctRuin,
            };
        }

        return new
        {
            evPorEvaluacion = defaults.Ev.EvPerEvaluation,
            evIc95 = new { inferior = defaults.Ev.CiLower, superior = defaults.Ev.CiUpper },
            muestraEvaluacionesTerminadas = defaults.Ev.SampleSize,
            kellyFraction = defaults.KellyFraction,
            escenarioEstandar,
        };
    }

    private async Task<AiReportDto> CallClaudeAndPersistAsync(AiReportKind kind, string? question, string userContent, CancellationToken ct)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "No hay una API key de Anthropic configurada. Define la variable de entorno " +
                "ANTHROPIC_API_KEY (o \"Ai:ApiKey\" en appsettings.Development.json / user-secrets) " +
                "y reinicia la aplicación. Ver README.md.");
        }

        var limits = await planService.GetLimitsAsync(ct: ct);
        var model = MapModel(limits.AiModelId);

        // Haiku 4.5 no soporta ni el pensamiento adaptativo ni el parámetro "effort" — la API
        // responde 400 ("adaptive thinking is not supported on this model" / "This model does
        // not support the effort parameter"). Solo Opus los soporta hoy, así que se activan
        // únicamente para ese modelo (Elite); Starter/Pro (Haiku) se quedan con los valores por
        // defecto del modelo.
        ThinkingConfigParam? thinking = null;
        OutputConfig? outputConfig = null;
        if (model == Model.ClaudeOpus4_8)
        {
            thinking = new ThinkingConfigAdaptive();
            outputConfig = new OutputConfig { Effort = Enum.Parse<Effort>(limits.AiEffort) };
        }

        var parameters = new MessageCreateParams
        {
            Model = model,
            MaxTokens = 4096,
            Thinking = thinking,
            OutputConfig = outputConfig,
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

        var userId = await currentUser.RequireUserIdAsync();
        var report = new AiReport
        {
            UserId = userId,
            Kind = kind,
            Question = question,
            Content = text,
            CreatedAt = DateTimeOffset.UtcNow,
            Model = limits.AiModelId,
            InputTokens = (int)response.Usage.InputTokens,
            OutputTokens = (int)response.Usage.OutputTokens,
        };

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.AiReports.Add(report);
        await db.SaveChangesAsync(ct);

        return new AiReportDto(report.Id, report.Kind, report.Question, report.Content, report.CreatedAt, report.Model, report.InputTokens, report.OutputTokens);
    }

    /// <summary>PlanLimits.AiModelId es el id "de cable" (para no acoplar Application al SDK de Anthropic); aquí se traduce al enum del SDK.</summary>
    private static Model MapModel(string modelId) => modelId switch
    {
        "claude-opus-4-8" => Model.ClaudeOpus4_8,
        "claude-haiku-4-5" => Model.ClaudeHaiku4_5,
        _ => Model.ClaudeHaiku4_5,
    };
}
