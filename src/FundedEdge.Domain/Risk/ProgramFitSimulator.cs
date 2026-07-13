using FundedEdge.Domain.Enums;

namespace FundedEdge.Domain.Risk;

/// <summary>
/// Parámetros de la simulación de ajuste (fit) de un programa de evaluación a la operativa real
/// del usuario. Extiende el Monte Carlo intra-cuenta (<see cref="AccountSimulator"/>) añadiendo las
/// reglas que de verdad diferencian a las firmas y que castigan de forma distinta a cada operativa:
/// pérdida diaria, días mínimos de trading y regla de consistencia.
///
/// A diferencia del simulador de cuenta, aquí los trades se agrupan en días de trading (de
/// <see cref="TradesPerDay"/> trades cada uno, derivado de la cadencia real del usuario) porque
/// esas tres reglas solo tienen sentido con estructura diaria.
/// </summary>
public record ProgramFitInput(
    IReadOnlyList<decimal> TradePnLs,       // Distribución empírica de PnL neto por trade (se muestrea con reemplazo)
    int TradesPerDay,                       // Trades por día de trading del usuario (>= 1)
    decimal ProfitTarget,
    decimal MaxDrawdown,
    DrawdownType DrawdownType,
    decimal? DailyLossLimit = null,         // null = la firma no impone tope de pérdida diaria
    int? MinTradingDays = null,             // null = sin mínimo de días
    decimal? ConsistencyMaxDayFraction = null, // null = sin regla de consistencia (fracción 0-1)
    int MaxTradingDays = 250,               // Corte de seguridad (~1 año de sesiones): ni pasa ni quema = "timeout"
    int Iterations = 10_000);

public record ProgramFitResult(
    double ProbabilityOfPassing,            // Pasa target + días mínimos + consistencia, sin quemarse
    double ProbabilityOfBusting,            // Quema el drawdown o el límite diario
    double ProbabilityOfTimeout,            // Agota MaxTradingDays sin resolverse
    double? AvgTradingDaysToPass);          // Media de días de trading hasta pasar; null si nunca pasa

/// <summary>
/// Monte Carlo por programa: muestrea días de trading (cada uno con <see cref="ProgramFitInput.TradesPerDay"/>
/// trades tomados con reemplazo de la distribución empírica) y aplica, en este orden, las reglas de
/// la firma. Semilla explícita para reproducibilidad en tests.
///
/// Modelo de reglas (comprobación de "pasar" solo a fin de día, para que la contabilidad diaria sea
/// consistente):
/// - Drawdown (trailing/EOD ≈ trailing a granularidad de trade; static ancla el suelo al inicio):
///   quema si el equity cae por debajo de pico − MaxDrawdown.
/// - Pérdida diaria: quema si dentro del día el equity cae DailyLossLimit por debajo del balance de
///   apertura del día.
/// - Días mínimos: no se puede pasar hasta acumular MinTradingDays días operados.
/// - Consistencia: al pasar, el día más rentable no puede superar ConsistencyMaxDayFraction del
///   beneficio total; si lo supera, no es un pase limpio y se sigue operando (arriesgando quemarla).
/// </summary>
public static class ProgramFitSimulator
{
    public static ProgramFitResult Simulate(ProgramFitInput input, int seed = 42)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(input.Iterations, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(input.ProfitTarget, 0m);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(input.MaxDrawdown, 0m);
        ArgumentOutOfRangeException.ThrowIfLessThan(input.TradesPerDay, 1);
        if (input.TradePnLs.Count == 0)
        {
            throw new ArgumentException("Se necesita al menos un trade histórico para muestrear.", nameof(input));
        }

        var rng = new Random(seed);
        int passed = 0, busted = 0, timedOut = 0;
        long daysToPassSum = 0;

        for (int i = 0; i < input.Iterations; i++)
        {
            var (outcome, tradingDays) = SimulatePath(input, rng);
            switch (outcome)
            {
                case PathOutcome.Passed:
                    passed++;
                    daysToPassSum += tradingDays;
                    break;
                case PathOutcome.Busted:
                    busted++;
                    break;
                default:
                    timedOut++;
                    break;
            }
        }

        return new ProgramFitResult(
            ProbabilityOfPassing: (double)passed / input.Iterations,
            ProbabilityOfBusting: (double)busted / input.Iterations,
            ProbabilityOfTimeout: (double)timedOut / input.Iterations,
            AvgTradingDaysToPass: passed > 0 ? (double)daysToPassSum / passed : null);
    }

    private enum PathOutcome { Passed, Busted, TimedOut }

    private static (PathOutcome Outcome, int TradingDays) SimulatePath(ProgramFitInput input, Random rng)
    {
        decimal equity = 0m, peak = 0m, maxDayProfit = 0m;
        int tradingDays = 0;
        var minDays = input.MinTradingDays ?? 0;

        for (int day = 0; day < input.MaxTradingDays; day++)
        {
            decimal dayStart = equity;

            for (int t = 0; t < input.TradesPerDay; t++)
            {
                equity += input.TradePnLs[rng.Next(input.TradePnLs.Count)];

                if (input.DrawdownType != DrawdownType.Static)
                {
                    peak = Math.Max(peak, equity);
                }

                // Drawdown máximo global.
                if (equity <= peak - input.MaxDrawdown)
                {
                    return (PathOutcome.Busted, tradingDays);
                }

                // Límite de pérdida diaria (caída desde la apertura del día).
                if (input.DailyLossLimit is { } dailyLimit && equity - dayStart <= -dailyLimit)
                {
                    return (PathOutcome.Busted, tradingDays);
                }
            }

            tradingDays++;
            maxDayProfit = Math.Max(maxDayProfit, equity - dayStart);

            // Comprobación de pase a fin de día: target + días mínimos + consistencia.
            if (equity >= input.ProfitTarget && tradingDays >= minDays && PassesConsistency(input, equity, maxDayProfit))
            {
                return (PathOutcome.Passed, tradingDays);
            }
        }

        return (PathOutcome.TimedOut, tradingDays);
    }

    /// <summary>
    /// El día más rentable no puede aportar más de la fracción permitida del beneficio total. Sin
    /// regla configurada o con beneficio no positivo (aún no se ha alcanzado el target), no aplica.
    /// </summary>
    private static bool PassesConsistency(ProgramFitInput input, decimal totalProfit, decimal maxDayProfit)
    {
        if (input.ConsistencyMaxDayFraction is not { } maxFraction || totalProfit <= 0m)
        {
            return true;
        }

        return maxDayProfit <= maxFraction * totalProfit;
    }
}
