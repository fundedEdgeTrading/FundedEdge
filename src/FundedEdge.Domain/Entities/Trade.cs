using FundedEdge.Domain.Common;
using FundedEdge.Domain.Enums;

namespace FundedEdge.Domain.Entities;

/// <summary>
/// Round-turn agregado (posición abierta y cerrada por completo), construido a partir de Executions.
/// En el MVP también se puede crear manualmente desde el journal.
/// </summary>
public class Trade : Entity
{
    public Guid AccountId { get; set; }
    public TradingAccount? Account { get; set; }

    public Guid? InstrumentId { get; set; }
    public Instrument? Instrument { get; set; }

    public string Symbol { get; set; } = null!;
    public TradeDirection Direction { get; set; }
    public int Quantity { get; set; }
    public decimal AvgEntryPrice { get; set; }
    public decimal AvgExitPrice { get; set; }
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset ClosedAt { get; set; }

    public decimal GrossPnL { get; set; }
    public decimal Commissions { get; set; }
    public decimal NetPnL => GrossPnL - Commissions;

    /// <summary>Riesgo asumido en la operación (distancia al stop x tamaño x valor del tick). Habilita el R-múltiplo.</summary>
    public decimal? RiskedAmount { get; set; }
    public decimal? RMultiple => RiskedAmount is > 0 ? NetPnL / RiskedAmount.Value : null;

    /// <summary>
    /// Máxima pérdida flotante (en $, valor positivo) alcanzada mientras el trade estaba abierto —
    /// auto-reportada por el trader (journal manual o mapeo de columnas al importar CSV), ya que no
    /// se captura histórico de precio intra-trade. Habilita MAE (Maximum Adverse Excursion).
    /// </summary>
    public decimal? MaxAdverseExcursion { get; set; }

    /// <summary>Máxima ganancia flotante (en $) alcanzada mientras el trade estaba abierto. Habilita MFE (Maximum Favorable Excursion).</summary>
    public decimal? MaxFavorableExcursion { get; set; }

    /// <summary>MAE expresado en R, igual que RMultiple. Cuánto "calor" (riesgo en contra) aguanta el trade antes de funcionar.</summary>
    public decimal? MaeR => RiskedAmount is > 0 && MaxAdverseExcursion is not null ? MaxAdverseExcursion / RiskedAmount.Value : null;

    /// <summary>MFE expresado en R.</summary>
    public decimal? MfeR => RiskedAmount is > 0 && MaxFavorableExcursion is not null ? MaxFavorableExcursion / RiskedAmount.Value : null;

    /// <summary>Fracción del movimiento favorable máximo que se llegó a capturar en el resultado final. 1.0 = saliste en el mejor punto posible.</summary>
    public decimal? CaptureRatio => MaxFavorableExcursion is > 0 ? NetPnL / MaxFavorableExcursion.Value : null;

    public string? Tags { get; set; }
    public string? Notes { get; set; }

    public List<Execution> Executions { get; set; } = [];
}
