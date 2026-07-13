namespace FundedEdge.Domain.Risk;

/// <summary>
/// Resultado neto de una evaluación terminada: para las fondeadas, payouts − coste de evaluación −
/// coste de activación; para las falladas/expiradas, −coste de evaluación. Es la unidad que se
/// remuestrea en el bootstrap.
/// </summary>
public record EvaluationOutcome(bool Funded, decimal NetResult);

public record EvEstimate(
    decimal EvPerEvaluation,
    decimal? CiLower,               // Intervalo de confianza 95 % (bootstrap); null si muestra < 2
    decimal? CiUpper,
    int SampleSize);

/// <summary>
/// Esperanza matemática del negocio de fondeo por evaluación comprada, con intervalo de confianza
/// por bootstrap sobre las evaluaciones históricas terminadas, y fracción de Kelly orientativa.
/// Ver GUIA_IMPLEMENTACION.md §10.1 y §10.4.
/// </summary>
public static class EvCalculator
{
    /// <summary>EV = P(pasar) × payout_medio − coste_evaluación − P(pasar) × coste_activación.</summary>
    public static decimal ComputeEvPerEvaluation(
        double passRate, decimal avgPayoutPerFundedAccount, decimal evaluationCost, decimal activationCost) =>
        (decimal)passRate * avgPayoutPerFundedAccount - evaluationCost - (decimal)passRate * activationCost;

    /// <summary>
    /// EV observado con IC 95 % por bootstrap (remuestreo con reemplazo de los resultados netos de
    /// cada evaluación terminada, B réplicas, percentiles 2.5/97.5).
    /// </summary>
    public static EvEstimate Estimate(IReadOnlyList<EvaluationOutcome> outcomes, int bootstrapReplicas = 2_000, int seed = 42)
    {
        if (outcomes.Count == 0)
        {
            return new EvEstimate(0m, null, null, 0);
        }

        decimal pointEstimate = outcomes.Average(o => o.NetResult);
        if (outcomes.Count < 2)
        {
            return new EvEstimate(pointEstimate, null, null, outcomes.Count);
        }

        var rng = new Random(seed);
        var means = new decimal[bootstrapReplicas];
        for (int b = 0; b < bootstrapReplicas; b++)
        {
            decimal sum = 0m;
            for (int i = 0; i < outcomes.Count; i++)
            {
                sum += outcomes[rng.Next(outcomes.Count)].NetResult;
            }
            means[b] = sum / outcomes.Count;
        }

        Array.Sort(means);
        return new EvEstimate(
            pointEstimate,
            CiLower: means[(int)(bootstrapReplicas * 0.025)],
            CiUpper: means[(int)(bootstrapReplicas * 0.975)],
            SampleSize: outcomes.Count);
    }

    /// <summary>
    /// Fracción de Kelly para la "apuesta" evaluación: se arriesga el coste de la evaluación con
    /// probabilidad de éxito = pass rate y ganancia neta media = payout − activación. Modelo
    /// binario (f* = p − q/b): orientativo, no una promesa — recomendar operar a ½ Kelly.
    /// Devuelve null si no hay edge (b ≤ 0 o f* ≤ 0).
    /// </summary>
    public static double? KellyFraction(double passRate, decimal avgPayoutPerFundedAccount, decimal evaluationCost, decimal activationCost)
    {
        if (passRate <= 0 || passRate > 1 || evaluationCost <= 0m)
        {
            return null;
        }

        double b = (double)((avgPayoutPerFundedAccount - activationCost) / evaluationCost);
        if (b <= 0)
        {
            return null;
        }

        double f = passRate - (1 - passRate) / b;
        return f > 0 ? f : null;
    }
}
