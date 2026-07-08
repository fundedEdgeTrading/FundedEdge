namespace TrackRecord.Application.Dtos;

/// <summary>
/// Impacto de una regla concreta de un programa sobre la probabilidad de pasarlo, para TU
/// operativa: cuánto sube P(pasar) si esa regla no existiera. Es el diferencial de Firm Fit —
/// no "esta firma tiene regla de consistencia" sino "esa regla te cuesta 14 puntos de pass rate".
/// </summary>
/// <param name="RuleKey">Clave estable de la regla para localizar su nombre en la UI (daily-loss, consistency, min-trading-days).</param>
/// <param name="PassProbabilityWithoutRule">P(pasar) simulada quitando solo esta regla.</param>
/// <param name="Delta">PassProbabilityWithoutRule − P(pasar) del programa; ≥ 0 = la regla te perjudica.</param>
public record RuleImpactDto(string RuleKey, double PassProbabilityWithoutRule, double Delta);

/// <summary>
/// Un programa de evaluación puntuado contra la operativa real del usuario: probabilidad de
/// pasarlo, EV por evaluación, coste esperado por cuenta fondeada y un Fit Score 0-100.
/// </summary>
public record FirmFitProgramDto(
    Guid ProgramId,
    string FirmName,
    string ProgramName,
    decimal AccountSize,
    decimal EvaluationCost,
    decimal ActivationCost,
    double PassProbability,
    decimal? EvPerEvaluation,            // null si el usuario no tiene payouts observados de los que estimar el ingreso
    decimal? CostPerFundedAccount,       // Coste esperado para conseguir una cuenta fondeada; null si P(pasar)=0
    double? AvgTradingDaysToPass,
    int FitScore,                        // 0-100: atractivo económico del programa para esta operativa
    IReadOnlyList<RuleImpactDto> RuleImpacts);

/// <summary>
/// Ranking Firm Fit completo con el contexto necesario para interpretarlo con honestidad
/// (muestra usada, confianza, si el plan limita la vista).
/// </summary>
public record FirmFitRankingDto(
    IReadOnlyList<FirmFitProgramDto> Programs,
    int TradesAnalyzed,
    int TradesPerDay,
    bool LowConfidence,                  // Muestra por debajo del umbral fiable: solo orientativo
    bool IsLimitedByPlan,                // Starter: solo se muestra el mejor programa
    decimal? AvgPayoutPerFundedAccount); // Ingreso medio observado usado para el EV; null si no hay payouts
