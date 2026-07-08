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
