using FundedEdge.Domain.Risk;

namespace FundedEdge.Application.Dtos;

/// <summary>
/// Valores observados en los datos reales del usuario, usados como defaults de la página /risk
/// y como entrada de las simulaciones cuando no se sobrescriben manualmente.
/// </summary>
public record RiskDefaultsDto(
    double? PassRate,
    decimal? AvgEvaluationCost,          // Media de costes Evaluation+Reset por cuenta terminada
    decimal? AvgActivationCost,          // Media de costes Activation sobre las cuentas fondeadas
    IReadOnlyList<decimal> PayoutsPerFundedAccount,
    int EvaluationsTerminated,
    int FundedAccounts,
    int TradesAvailable,
    EvEstimate? Ev,
    double? KellyFraction);

/// <summary>Petición del planner de bankroll. Los campos null usan el valor observado (RiskDefaultsDto).</summary>
public record BankrollPlanRequest(
    decimal Bankroll,
    int MonthlyEvaluationBudget,
    int Months,
    decimal? EvaluationCostOverride = null,
    decimal? ActivationCostOverride = null,
    double? PassRateOverride = null,
    int Iterations = 10_000);

public record BankrollPlanResult(
    RuinSimulationResult Simulation,
    decimal? MinimumBankrollFor5PctRuin,
    RuinSimulationInput InputsUsed);     // Transparencia: con qué números se simuló realmente

/// <summary>
/// Simulación intra-cuenta: probabilidad de alcanzar el profit target de la cuenta antes de
/// quemar su drawdown, con la distribución empírica de trades del usuario. En una cuenta en
/// evaluación ≈ P(pasar); en una fondeada ≈ P(llegar al primer payout sin quemarla).
/// </summary>
public record AccountRiskResultDto(
    Guid AccountId,
    string AccountDisplayName,
    AccountSimulationResult Simulation,
    int TradesSampled,
    bool UsedGlobalTrades);              // true si la cuenta no tenía trades y se muestreó el global

/// <summary>
/// Aviso de proximidad al drawdown de una cuenta activa (F5.4): cuánto del colchón permitido ya
/// se ha consumido según el equity real acumulado de sus trades y la regla de drawdown de la firma.
/// </summary>
public record DrawdownAlertDto(
    Guid AccountId,
    string AccountDisplayName,
    double ConsumedFraction,             // 0-1; >= 0.8 dispara el aviso
    decimal RemainingBuffer);            // Distancia en $ hasta el suelo de drawdown
