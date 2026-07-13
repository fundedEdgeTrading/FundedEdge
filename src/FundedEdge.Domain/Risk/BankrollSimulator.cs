namespace FundedEdge.Domain.Risk;

/// <summary>Parámetros de la simulación Monte Carlo del bankroll del negocio (GUIA_IMPLEMENTACION.md §10.2).</summary>
public record RuinSimulationInput(
    decimal Bankroll,               // Capital disponible para el negocio
    decimal EvaluationCost,         // Coste medio por evaluación (incl. resets prorrateados)
    decimal ActivationCost,
    double PassRate,                // De tus datos
    IReadOnlyList<decimal> HistoricalPayoutsPerFundedAccount, // Distribución empírica
    int MonthlyEvaluationBudget,    // Cuántas compras al mes como máximo
    int Months,
    int Iterations = 10_000);

public record RuinSimulationResult(
    double ProbabilityOfRuin,       // % de simulaciones que agotan el bankroll
    decimal MedianFinalBankroll,
    decimal P5FinalBankroll,        // Percentil 5 (escenario malo)
    decimal P95FinalBankroll,
    int? MedianMonthsToBreakeven);  // null si la mediana de caminos no recupera el bankroll inicial

/// <summary>
/// Monte Carlo del negocio de fondeo: cada mes se compran hasta N evaluaciones; cada una pasa con
/// probabilidad PassRate (Bernoulli) y, si pasa, paga la activación y cobra un payout muestreado
/// con reemplazo de la distribución empírica del usuario. "Ruina" = no queda capital para comprar
/// ni una evaluación más. Semilla explícita para reproducibilidad en tests.
/// </summary>
public static class BankrollSimulator
{
    public static RuinSimulationResult Simulate(RuinSimulationInput input, int seed = 42)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(input.Iterations, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(input.Months, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(input.EvaluationCost, 0m);
        ArgumentOutOfRangeException.ThrowIfNegative(input.ActivationCost);
        if (input.PassRate is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "PassRate debe estar en [0, 1].");
        }

        var rng = new Random(seed);
        var finalBankrolls = new decimal[input.Iterations];
        var monthsToBreakeven = new int[input.Iterations];
        int ruined = 0;

        for (int i = 0; i < input.Iterations; i++)
        {
            var (final, isRuined, breakevenMonth) = SimulatePath(input, rng);
            finalBankrolls[i] = final;
            monthsToBreakeven[i] = breakevenMonth;
            if (isRuined) ruined++;
        }

        Array.Sort(finalBankrolls);
        Array.Sort(monthsToBreakeven); // los caminos sin breakeven quedan al final como int.MaxValue

        int medianBreakeven = monthsToBreakeven[input.Iterations / 2];

        return new RuinSimulationResult(
            ProbabilityOfRuin: (double)ruined / input.Iterations,
            MedianFinalBankroll: Percentile(finalBankrolls, 0.50),
            P5FinalBankroll: Percentile(finalBankrolls, 0.05),
            P95FinalBankroll: Percentile(finalBankrolls, 0.95),
            MedianMonthsToBreakeven: medianBreakeven == int.MaxValue ? null : medianBreakeven);
    }

    /// <summary>
    /// Bankroll mínimo para que P(ruina) quede por debajo de <paramref name="maxRuinProbability"/>
    /// dentro del horizonte simulado, por búsqueda binaria sobre la simulación. Ojo: con EV
    /// negativo esto no mide viabilidad, solo el capital necesario para financiar la sangría
    /// durante el horizonte — la página /risk debe mostrarlo junto al semáforo de EV. Devuelve
    /// null solo en el caso degenerado de que ni la cota superior (financiar todas las compras
    /// posibles del horizonte) cumpla el umbral pedido.
    /// </summary>
    public static decimal? FindMinimumBankroll(RuinSimulationInput input, double maxRuinProbability = 0.05, int seed = 42)
    {
        decimal low = input.EvaluationCost;
        // Cota superior: capital para financiar todo el horizonte a presupuesto máximo sin ingresar nada.
        decimal high = (input.EvaluationCost + input.ActivationCost) * input.MonthlyEvaluationBudget * input.Months;

        if (RuinAt(high) > maxRuinProbability)
        {
            return null;
        }

        while (high - low > input.EvaluationCost / 4)
        {
            var mid = (low + high) / 2;
            if (RuinAt(mid) <= maxRuinProbability) high = mid;
            else low = mid;
        }

        return Math.Ceiling(high);

        double RuinAt(decimal bankroll) =>
            Simulate(input with { Bankroll = bankroll }, seed).ProbabilityOfRuin;
    }

    private static (decimal FinalBankroll, bool Ruined, int BreakevenMonth) SimulatePath(RuinSimulationInput input, Random rng)
    {
        decimal bankroll = input.Bankroll;
        var payouts = input.HistoricalPayoutsPerFundedAccount;
        int breakevenMonth = int.MaxValue;

        for (int month = 1; month <= input.Months; month++)
        {
            if (bankroll < input.EvaluationCost)
            {
                return (bankroll, true, breakevenMonth);
            }

            int purchases = Math.Min(input.MonthlyEvaluationBudget, (int)(bankroll / input.EvaluationCost));
            for (int p = 0; p < purchases; p++)
            {
                bankroll -= input.EvaluationCost;
                if (rng.NextDouble() < input.PassRate)
                {
                    bankroll -= input.ActivationCost;
                    // Sin histórico de payouts, una cuenta fondeada se asume a 0 (pesimista).
                    if (payouts.Count > 0)
                    {
                        bankroll += payouts[rng.Next(payouts.Count)];
                    }
                }
            }

            if (breakevenMonth == int.MaxValue && bankroll >= input.Bankroll)
            {
                breakevenMonth = month;
            }
        }

        return (bankroll, false, breakevenMonth);
    }

    private static decimal Percentile(decimal[] sorted, double percentile)
    {
        var index = (int)Math.Clamp(Math.Round(percentile * (sorted.Length - 1)), 0, sorted.Length - 1);
        return sorted[index];
    }
}
