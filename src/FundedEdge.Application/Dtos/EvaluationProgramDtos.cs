using FundedEdge.Domain.Enums;

namespace FundedEdge.Application.Dtos;

/// <summary>
/// DTO de lectura que representa un programa de evaluación completo del catálogo,
/// incluyendo las reglas de fase fondeada y las condiciones de payout.
/// </summary>
public record EvaluationProgramDto(
    Guid Id,
    Guid PropFirmId,
    string PropFirmName,
    string Name,
    decimal AccountSize,
    decimal EvaluationCost,
    decimal ActivationCost,
    // ── Fase evaluación ──────────────────────────────────────────────────────────
    decimal ProfitTarget,
    decimal MaxDrawdown,
    DrawdownType DrawdownType,
    decimal? DailyLossLimit,
    int? MinTradingDays,
    decimal? ConsistencyMaxDayFraction,
    // ── Fase fondeada ────────────────────────────────────────────────────────────
    decimal? FundedMaxDrawdown,
    DrawdownType? FundedDrawdownType,
    decimal? FundedDailyLossLimit,
    decimal? FundedProfitTarget,
    int? FundedMinTradingDays,
    // ── Payout ──────────────────────────────────────────────────────────────────
    decimal PayoutSplitTraderPct,
    decimal? PayoutMaxProfitPct,
    int? PayoutMinDaysBetween,
    // ── Versionado ──────────────────────────────────────────────────────────────
    DateOnly EffectiveFrom,
    bool IsActive);

/// <summary>
/// Request para crear o actualizar un programa de evaluación. Al actualizar, el servicio
/// marca el programa anterior como inactivo y crea uno nuevo con <c>EffectiveFrom = hoy</c>
/// (versionado sin pérdida de historial).
/// </summary>
public record UpsertEvaluationProgramRequest(
    Guid PropFirmId,
    string Name,
    decimal AccountSize,
    decimal EvaluationCost,
    decimal ActivationCost,
    // ── Fase evaluación ──────────────────────────────────────────────────────────
    decimal ProfitTarget,
    decimal MaxDrawdown,
    DrawdownType DrawdownType,
    decimal? DailyLossLimit,
    int? MinTradingDays,
    decimal? ConsistencyMaxDayFraction,
    // ── Fase fondeada ────────────────────────────────────────────────────────────
    decimal? FundedMaxDrawdown,
    DrawdownType? FundedDrawdownType,
    decimal? FundedDailyLossLimit,
    decimal? FundedProfitTarget,
    int? FundedMinTradingDays,
    // ── Payout ──────────────────────────────────────────────────────────────────
    decimal PayoutSplitTraderPct,
    decimal? PayoutMaxProfitPct,
    int? PayoutMinDaysBetween);
