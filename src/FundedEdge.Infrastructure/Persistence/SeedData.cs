using Microsoft.EntityFrameworkCore;
using FundedEdge.Domain.Entities;
using FundedEdge.Domain.Enums;

namespace FundedEdge.Infrastructure.Persistence;

/// <summary>
/// Datos de arranque: firmas prop y programas de evaluación. Se aplican vía HasData en
/// las migraciones (Ids fijos y deterministas para que el seed sea reproducible).
/// </summary>
public static class SeedData
{
    // ── Ids de firms originales ──────────────────────────────────────────────────────────────
    public static readonly Guid LucidTradingId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid TradeifyId     = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid ApexId         = Guid.Parse("33333333-3333-3333-3333-333333333333");

    // ── Ids de firms nuevas (futuros) ────────────────────────────────────────────────────────
    public static readonly Guid TopstepId            = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid MyFundedFuturesId    = Guid.Parse("55555555-5555-5555-5555-555555555555");
    public static readonly Guid TakeProfitTraderId   = Guid.Parse("66666666-6666-6666-6666-666666666666");
    public static readonly Guid Earn2TradeId         = Guid.Parse("77777777-7777-7777-7777-777777777777");

    /// <summary>
    /// Catálogo inicial de programas de evaluación para el módulo Firm Fit: todas las modalidades
    /// de cuenta (tamaños) de cada firma, con sus reglas de evaluación, fondeo y retiro completas,
    /// para que el motor de ranking pueda comparar y seleccionar automáticamente. Reglas orientativas
    /// a fecha de vigencia (recopiladas de fuentes públicas, julio 2026) — pendientes de verificación
    /// contra la web oficial de cada firma salvo que se indique "confirmado por usuario". Se versionan
    /// (IsActive/EffectiveFrom), no se borran. Ids fijos y deterministas para que el seed sea
    /// reproducible entre migración y snapshot.
    /// </summary>
    public static readonly EvaluationProgram[] Programs =
    [
        // ── Apex Trader Funding ──────────────────────────────────────────────────────────────
        // Evaluación: trailing drawdown, sin daily loss, regla consistencia 30%, ≥7 días trading.
        // Fondeada: mismo trailing drawdown, sin profit target, split 100%, ≥7 días entre payouts.
        // Lineup post-actualización 4.0 (marzo 2026): 25K/50K/100K/150K (retira 75K/250K/300K).
        new()
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000015"), PropFirmId = ApexId,
            Name = "Apex 25K", AccountSize = 25_000m, EvaluationCost = 147m, ActivationCost = 130m,
            ProfitTarget = 1_500m, MaxDrawdown = 1_500m, DrawdownType = DrawdownType.Trailing,
            DailyLossLimit = null, MinTradingDays = 7, ConsistencyMaxDayFraction = 0.30m,
            FundedMaxDrawdown = 1_500m, FundedDrawdownType = DrawdownType.Trailing,
            FundedDailyLossLimit = null, FundedProfitTarget = null, FundedMinTradingDays = 7,
            PayoutSplitTraderPct = 1.00m, PayoutMaxProfitPct = null, PayoutMinDaysBetween = 7,
            EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true,
        },
        new()
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000001"), PropFirmId = ApexId,
            Name = "Apex 50K", AccountSize = 50_000m, EvaluationCost = 167m, ActivationCost = 130m,
            ProfitTarget = 3_000m, MaxDrawdown = 2_500m, DrawdownType = DrawdownType.Trailing,
            DailyLossLimit = null, MinTradingDays = 7, ConsistencyMaxDayFraction = 0.30m,
            FundedMaxDrawdown = 2_500m, FundedDrawdownType = DrawdownType.Trailing,
            FundedDailyLossLimit = null, FundedProfitTarget = null, FundedMinTradingDays = 7,
            PayoutSplitTraderPct = 1.00m, PayoutMaxProfitPct = null, PayoutMinDaysBetween = 7,
            EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true,
        },
        new()
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000002"), PropFirmId = ApexId,
            Name = "Apex 100K", AccountSize = 100_000m, EvaluationCost = 207m, ActivationCost = 130m,
            ProfitTarget = 6_000m, MaxDrawdown = 3_000m, DrawdownType = DrawdownType.Trailing,
            DailyLossLimit = null, MinTradingDays = 7, ConsistencyMaxDayFraction = 0.30m,
            FundedMaxDrawdown = 3_000m, FundedDrawdownType = DrawdownType.Trailing,
            FundedDailyLossLimit = null, FundedProfitTarget = null, FundedMinTradingDays = 7,
            PayoutSplitTraderPct = 1.00m, PayoutMaxProfitPct = null, PayoutMinDaysBetween = 7,
            EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true,
        },
        new()
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000016"), PropFirmId = ApexId,
            Name = "Apex 150K", AccountSize = 150_000m, EvaluationCost = 297m, ActivationCost = 130m,
            ProfitTarget = 9_000m, MaxDrawdown = 5_000m, DrawdownType = DrawdownType.Trailing,
            DailyLossLimit = null, MinTradingDays = 7, ConsistencyMaxDayFraction = 0.30m,
            FundedMaxDrawdown = 5_000m, FundedDrawdownType = DrawdownType.Trailing,
            FundedDailyLossLimit = null, FundedProfitTarget = null, FundedMinTradingDays = 7,
            PayoutSplitTraderPct = 1.00m, PayoutMaxProfitPct = null, PayoutMinDaysBetween = 7,
            EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true,
        },

        // ── Tradeify (Growth Evaluation) ─────────────────────────────────────────────────────
        // EOD trailing drawdown, daily loss limit (soft breach), sin consistencia, ≥5 días trading.
        // Fondeada: mismo EOD drawdown, sin profit target, split 90/10, ≥14 días entre payouts.
        new()
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000017"), PropFirmId = TradeifyId,
            Name = "Tradeify Growth 25K", AccountSize = 25_000m, EvaluationCost = 130m, ActivationCost = 0m,
            ProfitTarget = 1_500m, MaxDrawdown = 1_000m, DrawdownType = DrawdownType.EndOfDay,
            DailyLossLimit = 600m, MinTradingDays = 5, ConsistencyMaxDayFraction = null,
            FundedMaxDrawdown = 1_000m, FundedDrawdownType = DrawdownType.EndOfDay,
            FundedDailyLossLimit = 600m, FundedProfitTarget = null, FundedMinTradingDays = null,
            PayoutSplitTraderPct = 0.90m, PayoutMaxProfitPct = null, PayoutMinDaysBetween = 14,
            EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true,
        },
        new()
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000003"), PropFirmId = TradeifyId,
            Name = "Tradeify Growth 50K", AccountSize = 50_000m, EvaluationCost = 165m, ActivationCost = 0m,
            ProfitTarget = 3_000m, MaxDrawdown = 2_000m, DrawdownType = DrawdownType.EndOfDay,
            DailyLossLimit = 1_250m, MinTradingDays = 5, ConsistencyMaxDayFraction = null,
            FundedMaxDrawdown = 2_000m, FundedDrawdownType = DrawdownType.EndOfDay,
            FundedDailyLossLimit = 1_250m, FundedProfitTarget = null, FundedMinTradingDays = null,
            PayoutSplitTraderPct = 0.90m, PayoutMaxProfitPct = null, PayoutMinDaysBetween = 14,
            EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true,
        },
        new()
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000004"), PropFirmId = TradeifyId,
            Name = "Tradeify Advanced 100K", AccountSize = 100_000m, EvaluationCost = 219m, ActivationCost = 0m,
            ProfitTarget = 6_000m, MaxDrawdown = 3_000m, DrawdownType = DrawdownType.EndOfDay,
            DailyLossLimit = 2_500m, MinTradingDays = 5, ConsistencyMaxDayFraction = null,
            FundedMaxDrawdown = 3_000m, FundedDrawdownType = DrawdownType.EndOfDay,
            FundedDailyLossLimit = 2_500m, FundedProfitTarget = null, FundedMinTradingDays = null,
            PayoutSplitTraderPct = 0.90m, PayoutMaxProfitPct = null, PayoutMinDaysBetween = 14,
            EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true,
        },
        new()
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000018"), PropFirmId = TradeifyId,
            Name = "Tradeify Growth 150K", AccountSize = 150_000m, EvaluationCost = 275m, ActivationCost = 0m,
            ProfitTarget = 9_000m, MaxDrawdown = 5_000m, DrawdownType = DrawdownType.EndOfDay,
            DailyLossLimit = 3_750m, MinTradingDays = 5, ConsistencyMaxDayFraction = null,
            FundedMaxDrawdown = 5_000m, FundedDrawdownType = DrawdownType.EndOfDay,
            FundedDailyLossLimit = 3_750m, FundedProfitTarget = null, FundedMinTradingDays = null,
            PayoutSplitTraderPct = 0.90m, PayoutMaxProfitPct = null, PayoutMinDaysBetween = 14,
            EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true,
        },

        // ── Lucid Trading (LucidFlex) ────────────────────────────────────────────────────────
        // Evaluación: static drawdown, daily loss limit, sin consistencia, sin mínimo días.
        // Fondeada: mismo static drawdown, sin profit target, split 90%, cap 50% del profit,
        //           ≥14 días entre payouts. (50K/100K confirmado por usuario.)
        new()
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000019"), PropFirmId = LucidTradingId,
            Name = "Lucid 25K", AccountSize = 25_000m, EvaluationCost = 95m, ActivationCost = 0m,
            ProfitTarget = 1_000m, MaxDrawdown = 1_000m, DrawdownType = DrawdownType.Static,
            DailyLossLimit = 625m, MinTradingDays = null, ConsistencyMaxDayFraction = null,
            FundedMaxDrawdown = 1_000m, FundedDrawdownType = DrawdownType.Static,
            FundedDailyLossLimit = 625m, FundedProfitTarget = null, FundedMinTradingDays = null,
            PayoutSplitTraderPct = 0.90m, PayoutMaxProfitPct = 0.50m, PayoutMinDaysBetween = 14,
            EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true,
        },
        new()
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000005"), PropFirmId = LucidTradingId,
            Name = "Lucid 50K", AccountSize = 50_000m, EvaluationCost = 137m, ActivationCost = 0m,
            ProfitTarget = 2_000m, MaxDrawdown = 2_000m, DrawdownType = DrawdownType.Static,
            DailyLossLimit = 1_250m, MinTradingDays = null, ConsistencyMaxDayFraction = null,
            FundedMaxDrawdown = 2_000m, FundedDrawdownType = DrawdownType.Static,
            FundedDailyLossLimit = 1_250m, FundedProfitTarget = null, FundedMinTradingDays = null,
            PayoutSplitTraderPct = 0.90m, PayoutMaxProfitPct = 0.50m, PayoutMinDaysBetween = 14,
            EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true,
        },
        new()
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000006"), PropFirmId = LucidTradingId,
            Name = "Lucid 100K", AccountSize = 100_000m, EvaluationCost = 267m, ActivationCost = 0m,
            ProfitTarget = 4_000m, MaxDrawdown = 3_000m, DrawdownType = DrawdownType.Static,
            DailyLossLimit = 2_500m, MinTradingDays = null, ConsistencyMaxDayFraction = null,
            FundedMaxDrawdown = 3_000m, FundedDrawdownType = DrawdownType.Static,
            FundedDailyLossLimit = 2_500m, FundedProfitTarget = null, FundedMinTradingDays = null,
            PayoutSplitTraderPct = 0.90m, PayoutMaxProfitPct = 0.50m, PayoutMinDaysBetween = 14,
            EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true,
        },
        new()
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000020"), PropFirmId = LucidTradingId,
            Name = "Lucid 150K", AccountSize = 150_000m, EvaluationCost = 365m, ActivationCost = 0m,
            ProfitTarget = 6_000m, MaxDrawdown = 4_500m, DrawdownType = DrawdownType.Static,
            DailyLossLimit = 3_750m, MinTradingDays = null, ConsistencyMaxDayFraction = null,
            FundedMaxDrawdown = 4_500m, FundedDrawdownType = DrawdownType.Static,
            FundedDailyLossLimit = 3_750m, FundedProfitTarget = null, FundedMinTradingDays = null,
            PayoutSplitTraderPct = 0.90m, PayoutMaxProfitPct = 0.50m, PayoutMinDaysBetween = 14,
            EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true,
        },

        // ── Topstep (Trading Combine) ────────────────────────────────────────────────────────
        // EOD trailing drawdown, daily loss limit, ≥5 días trading. Split 90/10 desde ene-2026,
        // payouts quincenales tras 5 días ganadores.
        new()
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000007"), PropFirmId = TopstepId,
            Name = "Topstep 50K", AccountSize = 50_000m, EvaluationCost = 165m, ActivationCost = 149m,
            ProfitTarget = 3_000m, MaxDrawdown = 2_000m, DrawdownType = DrawdownType.Trailing,
            DailyLossLimit = 1_000m, MinTradingDays = 5, ConsistencyMaxDayFraction = null,
            FundedMaxDrawdown = 2_000m, FundedDrawdownType = DrawdownType.Trailing,
            FundedDailyLossLimit = 1_000m, FundedProfitTarget = null, FundedMinTradingDays = null,
            PayoutSplitTraderPct = 0.90m, PayoutMaxProfitPct = null, PayoutMinDaysBetween = 14,
            EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true,
        },
        new()
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000008"), PropFirmId = TopstepId,
            Name = "Topstep 100K", AccountSize = 100_000m, EvaluationCost = 245m, ActivationCost = 149m,
            ProfitTarget = 6_000m, MaxDrawdown = 3_000m, DrawdownType = DrawdownType.Trailing,
            DailyLossLimit = 2_000m, MinTradingDays = 5, ConsistencyMaxDayFraction = null,
            FundedMaxDrawdown = 3_000m, FundedDrawdownType = DrawdownType.Trailing,
            FundedDailyLossLimit = 2_000m, FundedProfitTarget = null, FundedMinTradingDays = null,
            PayoutSplitTraderPct = 0.90m, PayoutMaxProfitPct = null, PayoutMinDaysBetween = 14,
            EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true,
        },
        new()
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000021"), PropFirmId = TopstepId,
            Name = "Topstep 150K", AccountSize = 150_000m, EvaluationCost = 199m, ActivationCost = 149m,
            ProfitTarget = 9_000m, MaxDrawdown = 4_500m, DrawdownType = DrawdownType.Trailing,
            DailyLossLimit = 3_000m, MinTradingDays = 5, ConsistencyMaxDayFraction = null,
            FundedMaxDrawdown = 4_500m, FundedDrawdownType = DrawdownType.Trailing,
            FundedDailyLossLimit = 3_000m, FundedProfitTarget = null, FundedMinTradingDays = null,
            PayoutSplitTraderPct = 0.90m, PayoutMaxProfitPct = null, PayoutMinDaysBetween = 14,
            EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true,
        },

        // ── MyFundedFutures (plan Rapid) ─────────────────────────────────────────────────────
        // EOD trailing drawdown, sin daily loss, regla consistencia 50%, sin mínimo días, split
        // 90/10, sin activación, payouts cada 5 días ganadores.
        new()
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000022"), PropFirmId = MyFundedFuturesId,
            Name = "MFF Rapid 25K", AccountSize = 25_000m, EvaluationCost = 120m, ActivationCost = 0m,
            ProfitTarget = 1_500m, MaxDrawdown = 1_000m, DrawdownType = DrawdownType.Trailing,
            DailyLossLimit = null, MinTradingDays = null, ConsistencyMaxDayFraction = 0.50m,
            FundedMaxDrawdown = 1_000m, FundedDrawdownType = DrawdownType.Trailing,
            FundedDailyLossLimit = null, FundedProfitTarget = null, FundedMinTradingDays = null,
            PayoutSplitTraderPct = 0.90m, PayoutMaxProfitPct = null, PayoutMinDaysBetween = 5,
            EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true,
        },
        new()
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000009"), PropFirmId = MyFundedFuturesId,
            Name = "MFF Rapid 50K", AccountSize = 50_000m, EvaluationCost = 165m, ActivationCost = 0m,
            ProfitTarget = 3_000m, MaxDrawdown = 2_000m, DrawdownType = DrawdownType.Trailing,
            DailyLossLimit = null, MinTradingDays = null, ConsistencyMaxDayFraction = 0.50m,
            FundedMaxDrawdown = 2_000m, FundedDrawdownType = DrawdownType.Trailing,
            FundedDailyLossLimit = null, FundedProfitTarget = null, FundedMinTradingDays = null,
            PayoutSplitTraderPct = 0.90m, PayoutMaxProfitPct = null, PayoutMinDaysBetween = 5,
            EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true,
        },
        new()
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000010"), PropFirmId = MyFundedFuturesId,
            Name = "MFF Rapid 100K", AccountSize = 100_000m, EvaluationCost = 250m, ActivationCost = 0m,
            ProfitTarget = 6_000m, MaxDrawdown = 3_000m, DrawdownType = DrawdownType.Trailing,
            DailyLossLimit = null, MinTradingDays = null, ConsistencyMaxDayFraction = 0.50m,
            FundedMaxDrawdown = 3_000m, FundedDrawdownType = DrawdownType.Trailing,
            FundedDailyLossLimit = null, FundedProfitTarget = null, FundedMinTradingDays = null,
            PayoutSplitTraderPct = 0.90m, PayoutMaxProfitPct = null, PayoutMinDaysBetween = 5,
            EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true,
        },
        new()
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000023"), PropFirmId = MyFundedFuturesId,
            Name = "MFF Rapid 150K", AccountSize = 150_000m, EvaluationCost = 320m, ActivationCost = 0m,
            ProfitTarget = 9_000m, MaxDrawdown = 4_500m, DrawdownType = DrawdownType.Trailing,
            DailyLossLimit = null, MinTradingDays = null, ConsistencyMaxDayFraction = 0.50m,
            FundedMaxDrawdown = 4_500m, FundedDrawdownType = DrawdownType.Trailing,
            FundedDailyLossLimit = null, FundedProfitTarget = null, FundedMinTradingDays = null,
            PayoutSplitTraderPct = 0.90m, PayoutMaxProfitPct = null, PayoutMinDaysBetween = 5,
            EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true,
        },

        // ── Take Profit Trader (cuenta PRO) ──────────────────────────────────────────────────
        // Evaluación: EOD trailing drawdown, sin daily loss, ≥5 días trading. Fondeada: pasa a
        // trailing intradía (cambio de tipo de drawdown al fondear). Split 80/20.
        new()
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000024"), PropFirmId = TakeProfitTraderId,
            Name = "TPT 25K", AccountSize = 25_000m, EvaluationCost = 150m, ActivationCost = 130m,
            ProfitTarget = 1_500m, MaxDrawdown = 1_500m, DrawdownType = DrawdownType.EndOfDay,
            DailyLossLimit = null, MinTradingDays = 5, ConsistencyMaxDayFraction = null,
            FundedMaxDrawdown = 1_500m, FundedDrawdownType = DrawdownType.Trailing,
            FundedDailyLossLimit = null, FundedProfitTarget = null, FundedMinTradingDays = null,
            PayoutSplitTraderPct = 0.80m, PayoutMaxProfitPct = null, PayoutMinDaysBetween = 14,
            EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true,
        },
        new()
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000011"), PropFirmId = TakeProfitTraderId,
            Name = "TPT 50K", AccountSize = 50_000m, EvaluationCost = 150m, ActivationCost = 130m,
            ProfitTarget = 3_000m, MaxDrawdown = 2_000m, DrawdownType = DrawdownType.EndOfDay,
            DailyLossLimit = null, MinTradingDays = 5, ConsistencyMaxDayFraction = null,
            FundedMaxDrawdown = 2_000m, FundedDrawdownType = DrawdownType.Trailing,
            FundedDailyLossLimit = null, FundedProfitTarget = null, FundedMinTradingDays = null,
            PayoutSplitTraderPct = 0.80m, PayoutMaxProfitPct = null, PayoutMinDaysBetween = 14,
            EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true,
        },
        new()
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000025"), PropFirmId = TakeProfitTraderId,
            Name = "TPT 75K", AccountSize = 75_000m, EvaluationCost = 185m, ActivationCost = 130m,
            ProfitTarget = 4_500m, MaxDrawdown = 3_000m, DrawdownType = DrawdownType.EndOfDay,
            DailyLossLimit = null, MinTradingDays = 5, ConsistencyMaxDayFraction = null,
            FundedMaxDrawdown = 3_000m, FundedDrawdownType = DrawdownType.Trailing,
            FundedDailyLossLimit = null, FundedProfitTarget = null, FundedMinTradingDays = null,
            PayoutSplitTraderPct = 0.80m, PayoutMaxProfitPct = null, PayoutMinDaysBetween = 14,
            EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true,
        },
        new()
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000012"), PropFirmId = TakeProfitTraderId,
            Name = "TPT 100K", AccountSize = 100_000m, EvaluationCost = 220m, ActivationCost = 130m,
            ProfitTarget = 6_000m, MaxDrawdown = 3_000m, DrawdownType = DrawdownType.EndOfDay,
            DailyLossLimit = null, MinTradingDays = 5, ConsistencyMaxDayFraction = null,
            FundedMaxDrawdown = 3_000m, FundedDrawdownType = DrawdownType.Trailing,
            FundedDailyLossLimit = null, FundedProfitTarget = null, FundedMinTradingDays = null,
            PayoutSplitTraderPct = 0.80m, PayoutMaxProfitPct = null, PayoutMinDaysBetween = 14,
            EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true,
        },
        new()
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000026"), PropFirmId = TakeProfitTraderId,
            Name = "TPT 150K", AccountSize = 150_000m, EvaluationCost = 300m, ActivationCost = 130m,
            ProfitTarget = 9_000m, MaxDrawdown = 4_500m, DrawdownType = DrawdownType.EndOfDay,
            DailyLossLimit = null, MinTradingDays = 5, ConsistencyMaxDayFraction = null,
            FundedMaxDrawdown = 4_500m, FundedDrawdownType = DrawdownType.Trailing,
            FundedDailyLossLimit = null, FundedProfitTarget = null, FundedMinTradingDays = null,
            PayoutSplitTraderPct = 0.80m, PayoutMaxProfitPct = null, PayoutMinDaysBetween = 14,
            EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true,
        },

        // ── Earn2Trade (Gauntlet Mini) ───────────────────────────────────────────────────────
        // EOD drawdown (evaluación y fondeada LiveSim), daily loss limit, ≥10 días trading, regla
        // consistencia 30%, split base 80/20 (el 50/50 por debajo de umbral de retiro no se modela:
        // limitación conocida del esquema, ver PayoutSplitTraderPct/EvaluationProgram).
        new()
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000014"), PropFirmId = Earn2TradeId,
            Name = "E2T Gauntlet Mini 50K", AccountSize = 50_000m, EvaluationCost = 245m, ActivationCost = 0m,
            ProfitTarget = 3_000m, MaxDrawdown = 2_000m, DrawdownType = DrawdownType.EndOfDay,
            DailyLossLimit = 1_100m, MinTradingDays = 10, ConsistencyMaxDayFraction = 0.30m,
            FundedMaxDrawdown = 2_000m, FundedDrawdownType = DrawdownType.EndOfDay,
            FundedDailyLossLimit = null, FundedProfitTarget = null, FundedMinTradingDays = null,
            PayoutSplitTraderPct = 0.80m, PayoutMaxProfitPct = null, PayoutMinDaysBetween = 30,
            EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true,
        },
        new()
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000013"), PropFirmId = Earn2TradeId,
            Name = "E2T Gauntlet Mini 100K", AccountSize = 100_000m, EvaluationCost = 430m, ActivationCost = 0m,
            ProfitTarget = 6_000m, MaxDrawdown = 4_000m, DrawdownType = DrawdownType.EndOfDay,
            DailyLossLimit = 2_200m, MinTradingDays = 10, ConsistencyMaxDayFraction = 0.30m,
            FundedMaxDrawdown = 4_000m, FundedDrawdownType = DrawdownType.EndOfDay,
            FundedDailyLossLimit = null, FundedProfitTarget = null, FundedMinTradingDays = null,
            PayoutSplitTraderPct = 0.80m, PayoutMaxProfitPct = null, PayoutMinDaysBetween = 30,
            EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true,
        },
        new()
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000027"), PropFirmId = Earn2TradeId,
            Name = "E2T Gauntlet Mini 150K", AccountSize = 150_000m, EvaluationCost = 600m, ActivationCost = 0m,
            ProfitTarget = 9_000m, MaxDrawdown = 6_000m, DrawdownType = DrawdownType.EndOfDay,
            DailyLossLimit = 3_300m, MinTradingDays = 10, ConsistencyMaxDayFraction = 0.30m,
            FundedMaxDrawdown = 6_000m, FundedDrawdownType = DrawdownType.EndOfDay,
            FundedDailyLossLimit = null, FundedProfitTarget = null, FundedMinTradingDays = null,
            PayoutSplitTraderPct = 0.80m, PayoutMaxProfitPct = null, PayoutMinDaysBetween = 30,
            EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true,
        },
        new()
        {
            Id = Guid.Parse("b0000000-0000-0000-0000-000000000028"), PropFirmId = Earn2TradeId,
            Name = "E2T Gauntlet Mini 200K", AccountSize = 200_000m, EvaluationCost = 750m, ActivationCost = 0m,
            ProfitTarget = 12_000m, MaxDrawdown = 8_000m, DrawdownType = DrawdownType.EndOfDay,
            DailyLossLimit = 4_400m, MinTradingDays = 10, ConsistencyMaxDayFraction = 0.30m,
            FundedMaxDrawdown = 8_000m, FundedDrawdownType = DrawdownType.EndOfDay,
            FundedDailyLossLimit = null, FundedProfitTarget = null, FundedMinTradingDays = null,
            PayoutSplitTraderPct = 0.80m, PayoutMaxProfitPct = null, PayoutMinDaysBetween = 30,
            EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true,
        },
    ];

    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PropFirm>().HasData(
            new PropFirm { Id = LucidTradingId, Name = "Lucid Trading", Website = "https://lucidtrading.com" },
            new PropFirm { Id = TradeifyId, Name = "Tradeify", Website = "https://tradeify.co" },
            new PropFirm { Id = ApexId, Name = "Apex Trader Funding", Website = "https://apextraderfunding.com" },
            new PropFirm { Id = TopstepId, Name = "Topstep", Website = "https://topstep.com" },
            new PropFirm { Id = MyFundedFuturesId, Name = "MyFundedFutures", Website = "https://myfundedfutures.com" },
            new PropFirm { Id = TakeProfitTraderId, Name = "Take Profit Trader", Website = "https://takeprofittrader.com" },
            new PropFirm { Id = Earn2TradeId, Name = "Earn2Trade", Website = "https://earn2trade.com" });

        modelBuilder.Entity<Instrument>().HasData(
            new Instrument { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"), Root = "ES", Name = "E-mini S&P 500", TickSize = 0.25m, TickValue = 12.50m },
            new Instrument { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002"), Root = "MES", Name = "Micro E-mini S&P 500", TickSize = 0.25m, TickValue = 1.25m },
            new Instrument { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003"), Root = "NQ", Name = "E-mini Nasdaq-100", TickSize = 0.25m, TickValue = 5.00m },
            new Instrument { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000004"), Root = "MNQ", Name = "Micro E-mini Nasdaq-100", TickSize = 0.25m, TickValue = 0.50m },
            new Instrument { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000005"), Root = "GC", Name = "Gold Futures", TickSize = 0.10m, TickValue = 10.00m },
            new Instrument { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000006"), Root = "CL", Name = "Crude Oil Futures", TickSize = 0.01m, TickValue = 10.00m });

        modelBuilder.Entity<EvaluationProgram>().HasData(Programs);
    }
}
