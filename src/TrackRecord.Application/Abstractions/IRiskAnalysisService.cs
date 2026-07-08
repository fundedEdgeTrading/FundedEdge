using TrackRecord.Application.Dtos;

namespace TrackRecord.Application.Abstractions;

/// <summary>
/// Módulo de riesgo (GUIA_IMPLEMENTACION.md §10): EV del funnel con intervalo de confianza,
/// Monte Carlo del bankroll del negocio y Monte Carlo intra-cuenta, siempre alimentados con los
/// datos reales del usuario (nada de supuestos genéricos).
/// </summary>
public interface IRiskAnalysisService
{
    /// <summary>Valores observados (pass rate, costes medios, payouts, EV, Kelly) para la página /risk.</summary>
    Task<RiskDefaultsDto> GetDefaultsAsync(CancellationToken ct = default);

    /// <summary>Simulación de ruina del bankroll + bankroll mínimo recomendado para P(ruina) &lt; 5 %.</summary>
    Task<BankrollPlanResult> RunBankrollPlanAsync(BankrollPlanRequest request, CancellationToken ct = default);

    /// <summary>
    /// Simulación de una cuenta concreta con la distribución empírica de trades. Devuelve null si
    /// la cuenta no existe o no hay ni un solo trade del que muestrear (ni suyo ni global).
    /// </summary>
    Task<AccountRiskResultDto?> RunAccountSimulationAsync(Guid accountId, CancellationToken ct = default);

    /// <summary>
    /// Cuentas activas (Evaluation/Funded) del usuario con >= 80 % del drawdown permitido
    /// consumido según su equity real acumulada. Para el aviso del dashboard (F5.4).
    /// </summary>
    Task<IReadOnlyList<DrawdownAlertDto>> GetDrawdownAlertsAsync(CancellationToken ct = default);
}
