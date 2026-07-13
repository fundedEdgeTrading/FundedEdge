using FundedEdge.Domain.Enums;

namespace FundedEdge.Application.Dtos;

/// <summary>
/// Progreso de una cuenta en fase de <b>Evaluación</b>. Incluye métricas calculadas contra
/// las reglas del programa (profit target, drawdown, consistencia, días trading) y la
/// probabilidad de pasar estimada por Monte Carlo.
/// </summary>
public record EvaluationProgressDto(
    // ── Profit target ────────────────────────────────────────────────────────────
    /// <summary>P&amp;L neto acumulado desde el inicio de la evaluación.</summary>
    decimal CurrentPnL,
    /// <summary>Profit target del programa.</summary>
    decimal ProfitTarget,
    /// <summary>Porcentaje completado del profit target (0-1+).</summary>
    double ProfitTargetPct,

    // ── Drawdown ─────────────────────────────────────────────────────────────────
    /// <summary>Drawdown consumido hasta ahora (valor positivo = pérdida desde el pico).</summary>
    decimal DrawdownConsumed,
    /// <summary>Drawdown máximo permitido por el programa.</summary>
    decimal MaxDrawdown,
    /// <summary>Porcentaje del drawdown consumido (0-1+). Color danger si &gt;0.80.</summary>
    double DrawdownConsumedPct,
    /// <summary>Tipo de drawdown del programa.</summary>
    DrawdownType DrawdownType,

    // ── Daily loss limit (null si el programa no la tiene) ───────────────────────
    decimal? DailyLossLimit,
    /// <summary>P&amp;L del día de hoy (positivo = ganancia, negativo = pérdida).</summary>
    decimal TodayPnL,

    // ── Consistencia (null si el programa no tiene la regla) ─────────────────────
    decimal? ConsistencyMaxDayFraction,
    /// <summary>Beneficio del mejor día hasta ahora.</summary>
    decimal BestDayPnL,
    /// <summary>Porcentaje que representa el mejor día sobre el P&amp;L total (0-1+).</summary>
    double BestDayFraction,

    // ── Días de trading ──────────────────────────────────────────────────────────
    int TradingDaysCompleted,
    int? MinTradingDays,

    // ── Monte Carlo ──────────────────────────────────────────────────────────────
    /// <summary>Probabilidad de pasar la evaluación estimada por Monte Carlo (0-1). Null si no hay suficientes datos.</summary>
    double? PassProbability);

/// <summary>
/// Progreso de una cuenta en fase <b>Fondeada</b>. Incluye profit acumulado desde el fondeo,
/// estimación del retiro máximo disponible y elegibilidad de payout.
/// </summary>
public record FundedProgressDto(
    // ── Profit desde fondeo ──────────────────────────────────────────────────────
    /// <summary>Beneficio neto acumulado desde FundedOn, descontados los payouts ya cobrados.</summary>
    decimal ProfitSinceFunded,
    /// <summary>Suma de todos los payouts ya recibidos/solicitados.</summary>
    decimal TotalPayoutsRequested,

    // ── Estimación del retiro máximo ─────────────────────────────────────────────
    /// <summary>Importe bruto máximo que se puede retirar (antes del split). Cap = min(profit × PayoutMaxProfitPct, buffer_drawdown).</summary>
    decimal MaxWithdrawalGross,
    /// <summary>Importe neto que recibiría el trader tras aplicar el split.</summary>
    decimal MaxWithdrawalNet,
    /// <summary>Fracción del split para el trader (ej. 0.90).</summary>
    decimal PayoutSplitTraderPct,
    /// <summary>Cap máximo aplicado como % del profit (null = sin cap).</summary>
    decimal? PayoutMaxProfitPct,

    // ── Drawdown ─────────────────────────────────────────────────────────────────
    decimal DrawdownBufferRemaining,
    decimal MaxDrawdown,
    DrawdownType DrawdownType,

    // ── Elegibilidad de payout ───────────────────────────────────────────────────
    /// <summary>True si ya se cumplen todos los requisitos para pedir un payout.</summary>
    bool IsPayoutEligible,
    /// <summary>Fecha desde la que el próximo payout será elegible. Null si ya es elegible o no hay regla de días.</summary>
    DateOnly? NextPayoutEligibleOn,
    /// <summary>Días de trading en la fase fondeada completados.</summary>
    int FundedTradingDaysCompleted,
    int? FundedMinTradingDays,

    // ── Consistencia (null si el programa no tiene la regla) ─────────────────────
    /// <summary>Fracción máxima del profit total que puede aportar un solo día (null = sin regla).</summary>
    decimal? ConsistencyMaxDayFraction,
    /// <summary>Beneficio del mejor día desde el fondeo.</summary>
    decimal BestDayPnL);

/// <summary>
/// Wrapper que contiene el progreso de una cuenta. Solo uno de los dos campos de progreso
/// estará informado según la etapa actual de la cuenta.
/// </summary>
public record AccountProgressDto(
    Guid AccountId,
    string AccountDisplayName,
    Guid PropFirmId,
    string PropFirmName,
    Guid EvaluationProgramId,
    string ProgramName,
    AccountStage Stage,
    EvaluationProgressDto? EvaluationProgress,
    FundedProgressDto? FundedProgress);
