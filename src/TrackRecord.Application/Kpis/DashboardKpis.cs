namespace TrackRecord.Application.Kpis;

/// <summary>
/// KPIs de negocio del funnel de fondeo (compra de evaluaciones → cuentas fondeadas → payouts).
/// Ver GUIA_IMPLEMENTACION.md §8.2.
/// </summary>
public record BusinessKpis(
    int AccountsPurchased,
    int AccountsInEvaluation,
    int AccountsFunded,
    int AccountsFailed,
    int AccountsWithdrawn,
    int AccountsExpired,
    int EvaluationsTerminated,   // Funded + Failed + Expired (fuera de Withdrawn, que no es resultado de evaluación)
    double? PassRate,            // AccountsFunded / EvaluationsTerminated, null si no hay muestra
    decimal TotalCosts,
    decimal TotalPayoutsReceived,
    decimal NetCashflow,
    decimal? CostPerFundedAccount,
    decimal? AvgPayoutPerFundedAccount,
    double? BusinessRoi);        // (payouts - costes) / costes

/// <summary>
/// KPIs de trading calculados sobre los Trades cerrados del filtro activo (cuenta/firma/global).
/// Ver GUIA_IMPLEMENTACION.md §8.1.
/// </summary>
public record TradingKpis(
    int TotalTrades,
    int WinningTrades,
    int LosingTrades,
    double? WinRate,
    decimal NetPnL,
    decimal GrossProfit,
    decimal GrossLoss,
    double? ProfitFactor,
    decimal AvgWin,
    decimal AvgLoss,
    double? PayoffRatio,
    decimal? Expectancy,
    double? AvgRMultiple,
    decimal MaxDrawdown,
    int MaxConsecutiveLosses,
    int MaxConsecutiveWins);

public record MonthlyCashflowPoint(int Year, int Month, decimal Costs, decimal Payouts, decimal Net);

public record EquityCurvePoint(DateOnly Date, decimal CumulativeNetPnL);

/// <summary>
/// Rendimiento agregado de un tag/setup (Trade.Tags, lista separada por comas) — "¿qué setup me
/// da dinero de verdad?". Un trade con varios tags cuenta en cada uno de ellos.
/// </summary>
public record TagPerformanceDto(
    string Tag,
    int TotalTrades,
    double? WinRate,
    decimal NetPnL,
    double? ProfitFactor);

/// <summary>
/// P&amp;L del negocio de fondeo pivotado por firma (GUIA_FUNCIONALIDADES_PROPUESTAS.md §3.1): la
/// cuenta de resultados de cada firma con la que se opera — ROI, coste medio por cuenta fondeada,
/// tiempo medio hasta el primer payout y tasa de quema.
/// </summary>
public record FirmBusinessBreakdownDto(
    Guid PropFirmId,
    string FirmName,
    int AccountsPurchased,
    int AccountsFunded,
    int AccountsFailed,
    int EvaluationsTerminated,
    double? PassRate,
    decimal TotalCosts,
    decimal TotalPayoutsReceived,
    decimal NetCashflow,
    decimal? CostPerFundedAccount,
    decimal? AvgPayoutPerFundedAccount,
    double? BusinessRoi,
    double? AvgDaysFundedToFirstPayout);

/// <summary>
/// Expectancy agregada por día de la semana y hora de entrada (GUIA_FUNCIONALIDADES_PROPUESTAS.md
/// §3.3) — "¿en qué franjas horarias opero mejor/peor?".
/// </summary>
public record TimeOfDayPerformancePoint(
    DayOfWeek DayOfWeek,
    int Hour,
    int TradeCount,
    double? WinRate,
    decimal? Expectancy,
    decimal NetPnL);

/// <summary>Duración media de ganadores vs perdedores — "corto ganadores, dejo correr perdedores" cuantificado.</summary>
public record DurationAsymmetryDto(
    double? AvgWinDurationMinutes,
    double? AvgLossDurationMinutes,
    int WinCount,
    int LossCount);

/// <summary>
/// Calidad de ejecución vía MAE/MFE (GUIA_FUNCIONALIDADES_PROPUESTAS.md §3.4): cuánto capturas del
/// movimiento favorable máximo y cuánto calor (adverso) aguantas antes de que el trade funcione.
/// Solo se calcula sobre los trades donde el usuario registró MAE/MFE — <see cref="CoveragePct"/>
/// indica qué fracción de la muestra los tiene.
/// </summary>
public record ExecutionQualityDto(
    int TradesWithData,
    double CoveragePct,
    double? AvgCaptureRatio,
    double? AvgMaeR,
    double? AvgMfeR);
