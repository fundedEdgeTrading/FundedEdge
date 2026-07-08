using TrackRecord.Domain.Common;
using TrackRecord.Domain.Enums;

namespace TrackRecord.Domain.Entities;

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

    public string? Tags { get; set; }
    public string? Notes { get; set; }

    public List<Execution> Executions { get; set; } = [];
}
