using FundedEdge.Domain.Enums;

namespace FundedEdge.Domain.Risk;

/// <summary>
/// Parámetros de la simulación de una cuenta individual (GUIA_IMPLEMENTACION.md §10.3): dada la
/// distribución empírica de PnL neto por trade del usuario y las reglas de la firma, ¿con qué
/// probabilidad se alcanza el target antes de quemar el drawdown?
/// </summary>
public record AccountSimulationInput(
    IReadOnlyList<decimal> TradePnLs,   // Distribución empírica (se muestrea con reemplazo)
    decimal ProfitTarget,
    decimal MaxDrawdown,
    DrawdownType DrawdownType,
    int MaxTradesPerPath = 1_000,       // Corte de seguridad: ni target ni ruina = "timeout"
    int Iterations = 10_000);

public record AccountSimulationResult(
    double ProbabilityOfReachingTarget,
    double ProbabilityOfBusting,
    double ProbabilityOfTimeout,        // Caminos que agotan MaxTradesPerPath sin resolverse
    double? AvgTradesToTarget,          // null si ningún camino alcanzó el target
    double? AvgTradesToBust);

/// <summary>
/// Monte Carlo por cuenta: muestrea trades con reemplazo de la distribución empírica y aplica la
/// regla de drawdown de la firma. Trailing y EndOfDay se aproximan ambos como trailing a
/// granularidad de trade (EndOfDay real es algo más permisivo intradía); Static ancla el suelo al
/// balance inicial. Semilla explícita para reproducibilidad.
/// </summary>
public static class AccountSimulator
{
    public static AccountSimulationResult Simulate(AccountSimulationInput input, int seed = 42)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(input.Iterations, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(input.ProfitTarget, 0m);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(input.MaxDrawdown, 0m);
        if (input.TradePnLs.Count == 0)
        {
            throw new ArgumentException("Se necesita al menos un trade histórico para muestrear.", nameof(input));
        }

        var rng = new Random(seed);
        int reached = 0, busted = 0, timedOut = 0;
        long tradesToTargetSum = 0, tradesToBustSum = 0;

        for (int i = 0; i < input.Iterations; i++)
        {
            decimal equity = 0m, peak = 0m;
            int trades = 0;

            while (true)
            {
                if (trades >= input.MaxTradesPerPath)
                {
                    timedOut++;
                    break;
                }

                equity += input.TradePnLs[rng.Next(input.TradePnLs.Count)];
                trades++;

                if (input.DrawdownType != DrawdownType.Static)
                {
                    peak = Math.Max(peak, equity);
                }

                if (equity <= peak - input.MaxDrawdown)
                {
                    busted++;
                    tradesToBustSum += trades;
                    break;
                }

                if (equity >= input.ProfitTarget)
                {
                    reached++;
                    tradesToTargetSum += trades;
                    break;
                }
            }
        }

        return new AccountSimulationResult(
            ProbabilityOfReachingTarget: (double)reached / input.Iterations,
            ProbabilityOfBusting: (double)busted / input.Iterations,
            ProbabilityOfTimeout: (double)timedOut / input.Iterations,
            AvgTradesToTarget: reached > 0 ? (double)tradesToTargetSum / reached : null,
            AvgTradesToBust: busted > 0 ? (double)tradesToBustSum / busted : null);
    }
}
