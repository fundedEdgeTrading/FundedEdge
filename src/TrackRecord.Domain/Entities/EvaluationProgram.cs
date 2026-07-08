using TrackRecord.Domain.Common;
using TrackRecord.Domain.Enums;

namespace TrackRecord.Domain.Entities;

/// <summary>
/// Un programa de evaluación concreto de una prop firm (p.ej. "Apex 50K", "Tradeify Growth 100K"):
/// su coste, su objetivo y las reglas exactas contra las que se simula la operativa real del
/// usuario en el módulo Firm Fit. A diferencia de <see cref="TradingAccount"/> —una cuenta que el
/// usuario ya posee— esto es una oferta del catálogo que el usuario todavía no ha comprado y que
/// el motor puntúa para recomendar qué comprar.
///
/// Versionado por <see cref="EffectiveFrom"/>/<see cref="IsActive"/>: las firmas cambian reglas y
/// precios, así que un programa nunca se borra; se marca inactivo y se crea otro con la fecha de
/// vigencia nueva. El ranking solo considera los activos.
/// </summary>
public class EvaluationProgram : Entity
{
    public Guid PropFirmId { get; set; }
    public PropFirm? PropFirm { get; set; }

    /// <summary>Nombre comercial del programa dentro de la firma (p.ej. "50K Static", "Growth 100K").</summary>
    public string Name { get; set; } = null!;

    public decimal AccountSize { get; set; }

    /// <summary>Coste de la evaluación (la cuota inicial que se paga para empezarla).</summary>
    public decimal EvaluationCost { get; set; }

    /// <summary>Cuota de activación al pasar a fondeada (0 si la firma no la cobra).</summary>
    public decimal ActivationCost { get; set; }

    public decimal ProfitTarget { get; set; }
    public decimal MaxDrawdown { get; set; }
    public DrawdownType DrawdownType { get; set; }

    /// <summary>
    /// Límite de pérdida diaria: si la caída intradía desde el balance de apertura del día supera
    /// este importe, la cuenta se quema ese día. Null = la firma no impone tope diario.
    /// </summary>
    public decimal? DailyLossLimit { get; set; }

    /// <summary>
    /// Días mínimos de trading para poder pasar/retirar. Null = sin mínimo. Alcanzar el target
    /// antes no basta: hay que seguir operando (con el riesgo de quemarla) hasta cumplirlo.
    /// </summary>
    public int? MinTradingDays { get; set; }

    /// <summary>
    /// Regla de consistencia como fracción (0-1): ningún día puede aportar más de este porcentaje
    /// del beneficio total al pasar (p.ej. 0.30 = "regla del 30 %" de Apex). Null = sin regla.
    /// </summary>
    public decimal? ConsistencyMaxDayFraction { get; set; }

    /// <summary>Fecha desde la que estas condiciones son válidas (para versionar cambios de la firma).</summary>
    public DateOnly EffectiveFrom { get; set; }

    /// <summary>Solo los programas activos entran en el ranking de Firm Fit.</summary>
    public bool IsActive { get; set; } = true;

    // ── Fase fondeada (null = igual que evaluación / no aplica) ─────────────────────────────────

    /// <summary>
    /// Drawdown máximo en la fase fondeada. Null = igual que en evaluación.
    /// Apex mantiene trailing drawdown igual; Lucid pasa a static; Tradeify mantiene EOD.
    /// </summary>
    public decimal? FundedMaxDrawdown { get; set; }

    /// <summary>
    /// Tipo de drawdown en la fase fondeada. Null = igual que en evaluación.
    /// </summary>
    public DrawdownType? FundedDrawdownType { get; set; }

    /// <summary>
    /// Límite de pérdida diaria en la fase fondeada. Null = no aplica o igual que en evaluación.
    /// </summary>
    public decimal? FundedDailyLossLimit { get; set; }

    /// <summary>
    /// Profit target en la fase fondeada. Null = sin límite superior (la cuenta fondeada no
    /// tiene objetivo de beneficio que cumplir; el retiro se solicita cuando el trader lo decida).
    /// </summary>
    public decimal? FundedProfitTarget { get; set; }

    /// <summary>
    /// Días mínimos de trading en la fase fondeada antes de poder solicitar el primer payout.
    /// Null = sin requisito mínimo de días.
    /// </summary>
    public int? FundedMinTradingDays { get; set; }

    // ── Reglas de payout ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fracción del beneficio que recibe el trader en cada payout (0-1).
    /// Ej.: 0.90 = trader cobra el 90 %, la firma retiene el 10 %.
    /// Por defecto 1.0 (trader se queda todo, sin split).
    /// </summary>
    public decimal PayoutSplitTraderPct { get; set; } = 1.0m;

    /// <summary>
    /// Porcentaje máximo del profit acumulado (desde fondeo menos payouts ya cobrados) que se
    /// puede retirar en un solo payout. Null = sin tope (se puede retirar todo el profit).
    /// Ej.: Lucid = 0.50 → máximo el 50 % del profit neto por retirada.
    /// </summary>
    public decimal? PayoutMaxProfitPct { get; set; }

    /// <summary>
    /// Días mínimos entre solicitudes de payout consecutivas. Null = sin restricción.
    /// Reemplaza <see cref="PropFirm.MinDaysBetweenPayouts"/> a nivel de programa
    /// (distintos programas de la misma firma pueden tener periodicidades diferentes).
    /// </summary>
    public int? PayoutMinDaysBetween { get; set; }
}
