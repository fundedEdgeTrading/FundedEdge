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
    IPsychologyService psychologyService,
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
        como ancla cuantitativa de la viabilidad, citando el intervalo y no solo el punto. Si hay
        un bloque "psicologia" (diario emocional auto-reportado por el trader), crúzalo con los
        KPIs de trading en una sección "Psicología": ¿las fugas de PnL coinciden con las emociones
        más caras? No lo trates como un dato menor.

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

    /// <summary>
    /// Tono para el informe dedicado de psicología (GUIA_PSICOLOGIA_TRADING.md §8.3): coach de
    /// rendimiento, no gurú; nombra el comportamiento sin juzgar a la persona; evidencia concreta
    /// antes que consejo genérico; una prioridad por informe; nunca lenguaje clínico/diagnóstico.
    /// </summary>
    private const string PsychologySystemPrompt =
        """
        Eres un coach de rendimiento para traders de futuros (en la tradición de Brett Steenbarger
        y Mark Douglas), no un gurú motivacional ni un profesional clínico. Recibirás estadísticas
        agregadas del diario emocional auto-reportado de un trader (cobertura, check-in diario,
        emociones más frecuentes, expectancy por emoción de entrada, disciplina, índice de tilt,
        coste emocional en R, detecciones activas de patrones de riesgo) cruzadas con sus KPIs de
        trading.

        Instrucciones de tono, estrictas:
        1. Valida la emoción, señala el comportamiento: nunca "hiciste mal", siempre "cuando
           aparece X, el patrón de trading es Y — y te cuesta Z".
        2. Evidencia concreta (números, frecuencias, trades citados por patrón) antes que consejo
           genérico. Cada afirmación debe apoyarse en un dato del contexto.
        3. Una sola prioridad de trabajo por informe, no una lista de diez. Elige la emoción o el
           patrón con mayor coste medible y profundiza en ella.
        4. Nunca uses lenguaje clínico ni de diagnóstico (nada de "trastorno", "patología",
           "adicción"). Si detectas señales de malestar sostenido (fatiga/burnout recurrente,
           racha emocional negativa prolongada), sugiere con tacto apoyo profesional — sin
           alarmismo y dejando claro que esto es coaching de rendimiento, no tratamiento.
        5. Si la cobertura del diario es baja (<30%), dilo explícitamente: las conclusiones son
           orientativas, no definitivas, y anima a registrar más para afinarlas.

        Responde siempre en español y en Markdown.
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

    public async Task<AiReportDto> GeneratePsychologyReportAsync(CancellationToken ct = default)
    {
        await EnsureAllowedAsync(AiReportKind.PsychologyAnalysis, ct);
        var psychology = await BuildPsychologyContextAsync(ct);
        if (psychology is null)
        {
            throw new InvalidOperationException(
                "Todavía no tienes diario emocional registrado. Registra las emociones de al menos unos trades en /psychology antes de pedir este informe.");
        }

        var trading = await kpiService.GetTradingKpisAsync(ct: ct);
        var payload = JsonSerializer.Serialize(
            new { psicologia = psychology, trading },
            new JsonSerializerOptions { WriteIndented = true });

        return await CallClaudeAndPersistAsync(AiReportKind.PsychologyAnalysis, null, payload, ct, PsychologySystemPrompt);
    }

    public async Task<AiReportDto> GenerateEventReportAsync(AiReportKind eventKind, string eventContext, CancellationToken ct = default)
    {
        await EnsureAllowedAsync(eventKind, ct);
        var payload = await BuildStatsPayloadAsync(ct);
        var instruction = eventKind switch
        {
            AiReportKind.LosingStreakAlert =>
                "El trader lleva una racha de al menos 3 días consecutivos con resultado negativo. " +
                "Genera un mini-informe de contención (máximo 200 palabras): qué está pasando según los datos " +
                "y 1-2 acciones concretas para frenar el sangrado hoy. Sé directo, sin rodeos.",
            AiReportKind.DrawdownRiskAlert =>
                "Una cuenta del trader está a menos del 20% de su colchón de drawdown restante — riesgo real de " +
                "quemarla. Genera un plan de emergencia breve (máximo 200 palabras): qué debe dejar de hacer ahora " +
                "mismo para no perder la cuenta.",
            AiReportKind.FirstPayoutMilestone =>
                "El trader acaba de cobrar su primer payout de una cuenta fondeada. Genera un informe de " +
                "consolidación breve (máximo 250 palabras): qué hizo distinto en este período según los datos, " +
                "para que lo identifique y lo repita.",
            _ => throw new ArgumentOutOfRangeException(nameof(eventKind), eventKind, "Tipo de evento no soportado."),
        };

        var userContent = $"{payload}\n\nEvento disparador: {eventContext}\n\n{instruction}";
        return await CallClaudeAndPersistAsync(eventKind, null, userContent, ct);
    }

    /// <summary>Lanza si el usuario ha agotado el cupo de IA de su plan (informes completos o preguntas ad-hoc).</summary>
    private async Task EnsureAllowedAsync(AiReportKind kind, CancellationToken ct)
    {
        var allowance = await planService.GetAiAllowanceAsync(ct);

        var isReportKind = kind is AiReportKind.Analysis or AiReportKind.PsychologyAnalysis
            or AiReportKind.LosingStreakAlert or AiReportKind.DrawdownRiskAlert or AiReportKind.FirstPayoutMilestone;

        if (isReportKind && !allowance.CanGenerateReport)
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
        var psicologia = await BuildPsychologyContextAsync(ct);

        return JsonSerializer.Serialize(
            new { negocio = business, trading, riesgo, psicologia },
            new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Bloque de psicología (GUIA_PSICOLOGIA_TRADING.md §8.1): agregados de los últimos 30 días,
    /// nunca los logs crudos completos, para controlar tokens. Null si el usuario no tiene ningún
    /// trade con diario emocional en la ventana — el informe sigue funcionando sin este bloque.
    /// </summary>
    private async Task<object?> BuildPsychologyContextAsync(CancellationToken ct)
    {
        var to = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var from = to.AddDays(-30);

        var metrics = await psychologyService.GetMetricsAsync(from, to, ct);
        if (metrics.CoveragePct <= 0)
        {
            return null;
        }

        var analytics = await psychologyService.GetAnalyticsAsync(from, to, ct);

        var emocionesFrecuentes = analytics.EmotionFrequency
            .GroupBy(e => e.Emotion)
            .Select(g => new { emocion = g.Key.ToString(), frecuencia = g.Sum(e => e.Count) })
            .OrderByDescending(e => e.frecuencia)
            .Take(5)
            .ToList();

        var expectancyPorEmocion = analytics.EmotionPerformance
            .OrderByDescending(e => e.TradeCount)
            .Take(6)
            .Select(e => new { emocion = e.Emotion.ToString(), avgR = e.AvgRMultiple, n = e.TradeCount, winRate = e.WinRate })
            .ToList();

        return new
        {
            coberturaDiarioEmocionalPct = Math.Round(metrics.CoveragePct * 100, 0),
            rachaDeRegistroDias = metrics.JournalStreakDays,
            emocionesEntradaMasFrecuentes = emocionesFrecuentes,
            expectancyPorEmocionEntrada = expectancyPorEmocion,
            disciplinaPlanSeguidoPct = Math.Round(
                analytics.DisciplineTrend.Count > 0 ? analytics.DisciplineTrend.Average(d => d.FollowedPlanRate) * 100 : 0, 0),
            indiceTilt = metrics.TiltIndex,
            scoreDisciplina = metrics.DisciplineScore,
            costeEmocionalPorR = metrics.EmotionalCostPerR,
            deteccionesActivas = metrics.Insights.Select(i => new { i.Title, severidad = i.Severity.ToString() }).ToList(),
        };
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

    private async Task<AiReportDto> CallClaudeAndPersistAsync(AiReportKind kind, string? question, string userContent, CancellationToken ct, string? systemPromptOverride = null)
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
                new() { Text = systemPromptOverride ?? SystemPrompt, CacheControl = new CacheControlEphemeral() },
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
