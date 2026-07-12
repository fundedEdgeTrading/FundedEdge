namespace TrackRecord.Application.Dtos;

/// <summary>
/// Cálculos derivados de la regla de consistencia (fracción máxima del profit total que puede
/// aportar un solo día). Compartidos por todas las páginas/KPIs que muestran el objetivo de profit.
/// </summary>
public static class ConsistencyMath
{
    /// <summary>
    /// Profit total mínimo requerido para que el mejor día no supere la fracción <paramref name="fraction"/>
    /// del total (mejorDía / fracción). 0 si no hay regla o no hay mejor día positivo.
    /// </summary>
    public static decimal RequiredProfit(decimal? fraction, decimal bestDayPnL)
        => fraction is > 0m && bestDayPnL > 0m ? bestDayPnL / fraction.Value : 0m;

    /// <summary>
    /// Objetivo de profit efectivo: el mayor entre el target base y el mínimo que exige la
    /// regla de consistencia con el mejor día actual.
    /// </summary>
    public static decimal EffectiveProfitTarget(decimal profitTarget, decimal? fraction, decimal bestDayPnL)
        => Math.Max(profitTarget, RequiredProfit(fraction, bestDayPnL));
}
