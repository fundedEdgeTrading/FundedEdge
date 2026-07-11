using System.Globalization;
using TrackRecord.Application.Dtos;
using TrackRecord.Domain.Entities;

namespace TrackRecord.Infrastructure.RuleMonitor;

/// <summary>
/// Diff campo a campo entre las reglas extraídas y el programa activo del catálogo.
/// Solo los campos que la extracción encontró (no null) cuentan: un null significa "la página
/// no lo menciona", nunca "borrar el valor actual". Sin programa existente, cada campo extraído
/// es un diff contra "—" (programa nuevo).
/// </summary>
public static class ProgramDiffCalculator
{
    public static IReadOnlyList<ProgramFieldDiff> ComputeDiffs(ExtractedProgramRules rules, EvaluationProgram? existing)
    {
        var diffs = new List<ProgramFieldDiff>();

        AddDecimal(diffs, rules, "accountSize", rules.AccountSize, existing?.AccountSize);
        AddDecimal(diffs, rules, "evaluationCost", rules.EvaluationCost, existing?.EvaluationCost);
        AddDecimal(diffs, rules, "activationCost", rules.ActivationCost, existing?.ActivationCost);
        AddDecimal(diffs, rules, "profitTarget", rules.ProfitTarget, existing?.ProfitTarget);
        AddDecimal(diffs, rules, "maxDrawdown", rules.MaxDrawdown, existing?.MaxDrawdown);
        AddEnum(diffs, rules, "drawdownType", rules.DrawdownType, existing?.DrawdownType);
        AddDecimal(diffs, rules, "dailyLossLimit", rules.DailyLossLimit, existing?.DailyLossLimit);
        AddInt(diffs, rules, "minTradingDays", rules.MinTradingDays, existing?.MinTradingDays);
        AddDecimal(diffs, rules, "consistencyMaxDayFraction", rules.ConsistencyMaxDayFraction, existing?.ConsistencyMaxDayFraction);
        AddDecimal(diffs, rules, "fundedMaxDrawdown", rules.FundedMaxDrawdown, existing?.FundedMaxDrawdown);
        AddEnum(diffs, rules, "fundedDrawdownType", rules.FundedDrawdownType, existing?.FundedDrawdownType);
        AddDecimal(diffs, rules, "fundedDailyLossLimit", rules.FundedDailyLossLimit, existing?.FundedDailyLossLimit);
        AddDecimal(diffs, rules, "fundedProfitTarget", rules.FundedProfitTarget, existing?.FundedProfitTarget);
        AddInt(diffs, rules, "fundedMinTradingDays", rules.FundedMinTradingDays, existing?.FundedMinTradingDays);
        AddDecimal(diffs, rules, "payoutSplitTraderPct", rules.PayoutSplitTraderPct, existing?.PayoutSplitTraderPct);
        AddDecimal(diffs, rules, "payoutMaxProfitPct", rules.PayoutMaxProfitPct, existing?.PayoutMaxProfitPct);
        AddInt(diffs, rules, "payoutMinDaysBetween", rules.PayoutMinDaysBetween, existing?.PayoutMinDaysBetween);

        return diffs;
    }

    private static void AddDecimal(List<ProgramFieldDiff> diffs, ExtractedProgramRules rules, string field, decimal? proposed, decimal? current)
    {
        if (proposed is null || proposed == current) return;
        diffs.Add(new ProgramFieldDiff(field, Format(current), Format(proposed)!, QuoteFor(rules, field)));
    }

    private static void AddInt(List<ProgramFieldDiff> diffs, ExtractedProgramRules rules, string field, int? proposed, int? current)
    {
        if (proposed is null || proposed == current) return;
        diffs.Add(new ProgramFieldDiff(field, current?.ToString(), proposed.Value.ToString(), QuoteFor(rules, field)));
    }

    private static void AddEnum<T>(List<ProgramFieldDiff> diffs, ExtractedProgramRules rules, string field, T? proposed, T? current) where T : struct, Enum
    {
        if (proposed is null || EqualityComparer<T?>.Default.Equals(proposed, current)) return;
        diffs.Add(new ProgramFieldDiff(field, current?.ToString(), proposed.Value.ToString(), QuoteFor(rules, field)));
    }

    private static string? Format(decimal? value) =>
        value?.ToString("0.####", CultureInfo.InvariantCulture);

    private static string? QuoteFor(ExtractedProgramRules rules, string field) =>
        rules.Quotes.FirstOrDefault(q => string.Equals(q.Field, field, StringComparison.OrdinalIgnoreCase))?.Quote;
}
