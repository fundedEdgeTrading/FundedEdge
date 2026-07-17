using FundedEdge.Domain.Enums;

namespace FundedEdge.Domain.Risk;

/// <summary>Trade reducido a lo mínimo que necesita el motor de cumplimiento: cierre y PnL neto.</summary>
public record ComplianceTrade(DateTime ClosedAt, decimal NetPnL);

/// <summary>
/// UsedToday siempre está informado (es un hecho: cuánto se ha perdido hoy), incluso si la firma no
/// impone límite diario; Remaining/ConsumedFraction son null cuando no hay límite que aplicar.
/// </summary>
public record DailyLossEvaluation(decimal UsedToday, decimal? Remaining, double? ConsumedFraction);

public record DrawdownEvaluation(decimal RemainingBuffer, double ConsumedFraction);

/// <summary>Null (a través del método que lo produce) cuando la firma no impone la regla o aún no hay ningún día rentable.</summary>
public record ConsistencyEvaluation(double TopDayFraction, double ConsumedFraction);

/// <summary>
/// Reglas de cumplimiento en tiempo real de una cuenta (semáforo de RuleComplianceService,
/// GUIA_FUNCIONALIDADES_PROPUESTAS.md §2.2/§3.5): cada regla -drawdown, pérdida diaria,
/// consistencia- es una función pura e independiente sobre el histórico de trades, así que añadir
/// o ajustar una no obliga a tocar las demás (SRP). No conocen el DTO de presentación ni el
/// semáforo de colores: solo calculan cuánto margen queda; RuleComplianceService traduce eso a
/// ComplianceLevel con sus umbrales.
/// </summary>
public static class ComplianceRuleEngine
{
    /// <summary>
    /// Drawdown (estático o trailing): colchón entre el equity actual y el suelo antes de quemar la
    /// cuenta. Trailing/EndOfDay siguen el pico histórico de equity; Static ancla el suelo al
    /// balance inicial. Siempre aplica: toda cuenta tiene un drawdown máximo.
    /// </summary>
    public static DrawdownEvaluation EvaluateDrawdown(
        IReadOnlyList<ComplianceTrade> trades, decimal maxDrawdown, DrawdownType drawdownType)
    {
        decimal equity = 0m, peak = 0m;
        foreach (var t in trades)
        {
            equity += t.NetPnL;
            if (drawdownType != DrawdownType.Static)
            {
                peak = Math.Max(peak, equity);
            }
        }

        var floor = peak - maxDrawdown;
        var remaining = Math.Max(equity - floor, 0m);
        var consumedFraction = maxDrawdown > 0 ? Math.Clamp(1 - (double)(remaining / maxDrawdown), 0, 1) : 0;
        return new DrawdownEvaluation(remaining, consumedFraction);
    }

    /// <summary>Pérdida diaria: lo perdido hoy frente al límite de la firma (null si no impone tope diario).</summary>
    public static DailyLossEvaluation EvaluateDailyLoss(
        IReadOnlyList<ComplianceTrade> trades, decimal? dailyLossLimit, DateOnly today)
    {
        var usedToday = Math.Max(
            -trades.Where(t => DateOnly.FromDateTime(t.ClosedAt.Date) == today).Sum(t => t.NetPnL), 0m);

        if (dailyLossLimit is not > 0)
            return new DailyLossEvaluation(usedToday, null, null);

        var remaining = dailyLossLimit.Value - usedToday;
        var consumedFraction = Math.Clamp((double)(usedToday / dailyLossLimit.Value), 0, 1);
        return new DailyLossEvaluation(usedToday, remaining, consumedFraction);
    }

    /// <summary>
    /// Consistencia: fracción del beneficio total aportada por el mejor día. Null si la firma no
    /// impone la regla, o si todavía no hay ningún día rentable con el que evaluarla (no es lo
    /// mismo que "regla cumplida": simplemente no hay datos aún).
    /// </summary>
    public static ConsistencyEvaluation? EvaluateConsistency(
        IReadOnlyList<ComplianceTrade> trades, decimal? maxDayFraction)
    {
        if (maxDayFraction is not > 0) return null;

        var dailyProfits = trades
            .GroupBy(t => DateOnly.FromDateTime(t.ClosedAt.Date))
            .Select(g => g.Sum(t => t.NetPnL))
            .Where(p => p > 0)
            .ToList();
        var totalProfit = dailyProfits.Sum();
        if (totalProfit <= 0) return null;

        var topDayFraction = (double)(dailyProfits.Max() / totalProfit);
        var consumedFraction = Math.Clamp(topDayFraction / (double)maxDayFraction.Value, 0, 1);
        return new ConsistencyEvaluation(topDayFraction, consumedFraction);
    }
}
