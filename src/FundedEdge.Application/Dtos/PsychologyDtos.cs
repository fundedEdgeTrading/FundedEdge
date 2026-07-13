using FundedEdge.Domain.Enums;
using FundedEdge.Domain.Psychology;

namespace FundedEdge.Application.Dtos;

/// <summary>Trade del usuario todavía sin diario emocional, con el contexto mínimo para mostrarlo en el formulario.</summary>
public record PendingEmotionTradeDto(
    Guid TradeId,
    string Symbol,
    DateTimeOffset OpenedAt,
    DateTimeOffset ClosedAt,
    decimal NetPnL,
    decimal? RMultiple);

/// <summary>Una emoción seleccionada en el formulario, con su intensidad 1-5.</summary>
public record EmotionEntryDto(EmotionType Emotion, int Intensity);

/// <summary>Diario emocional completo de un trade (R1): emociones antes/después, disciplina e impulsividad.</summary>
public record SaveTradeEmotionsRequest(
    Guid TradeId,
    IReadOnlyList<EmotionEntryDto> BeforeEntry,
    IReadOnlyList<EmotionEntryDto> AfterExit,
    PlanAdherence Adherence,
    bool WasImpulsive,
    string? Note);

/// <summary>Check-in diario de estado general (§3, §5 paso 1).</summary>
public record DailyCheckInDto(
    DateOnly Date,
    int SleepQuality,
    int ExternalStress,
    int PreMarketFocus,
    EmotionType DominantPreMarketEmotion,
    string? Note);

/// <summary>Frecuencia e intensidad media de una emoción en una semana — para el radar/barras (R3).</summary>
public record EmotionFrequencyPoint(DateOnly WeekStart, EmotionType Emotion, int Count, double AvgIntensity);

/// <summary>Rendimiento agregado por emoción de entrada — la gráfica "cuánto te cuesta cada emoción" (R3/R4).</summary>
public record EmotionPerformancePoint(EmotionType Emotion, int TradeCount, double? WinRate, decimal? AvgRMultiple, decimal NetPnL);

/// <summary>Un día del calendario emocional: valencia media y PnL del día (R3).</summary>
public record MoodCalendarDay(DateOnly Date, double AvgValence, decimal NetPnL, bool HasCheckIn);

/// <summary>% de trades con plan seguido por semana — tendencia de disciplina (R3).</summary>
public record DisciplineTrendPoint(DateOnly WeekStart, double FollowedPlanRate);

/// <summary>Agregados para las gráficas de /psychology (R3).</summary>
public record EmotionAnalyticsDto(
    IReadOnlyList<EmotionFrequencyPoint> EmotionFrequency,
    IReadOnlyList<EmotionPerformancePoint> EmotionPerformance,
    IReadOnlyList<MoodCalendarDay> MoodCalendar,
    IReadOnlyList<DisciplineTrendPoint> DisciplineTrend,
    IReadOnlyList<EmotionalCapitalPoint> EmotionalCapitalTrend);

/// <summary>Métricas derivadas + insights activos (R4/R5), listas para el dashboard, la IA y las alertas.</summary>
public record PsychMetricsDto(
    int TiltIndex,
    int DisciplineScore,
    decimal? EmotionalCostPerR,
    int JournalStreakDays,
    double CoveragePct,
    IReadOnlyList<PsychInsight> Insights);
