using FundedEdge.Domain.Common;
using FundedEdge.Domain.Enums;

namespace FundedEdge.Domain.Entities;

/// <summary>
/// Fill crudo e inmutable tal y como llega de la fuente (Tradovate, NinjaTrader, CSV o manual).
/// Base para reconstruir Trades agregados.
/// </summary>
public class Execution : Entity
{
    public Guid AccountId { get; set; }
    public TradingAccount? Account { get; set; }

    /// <summary>Identificador en el sistema de origen; junto con Source garantiza idempotencia al importar.</summary>
    public string ExternalId { get; set; } = null!;
    public TradeSourceType Source { get; set; }

    public string Symbol { get; set; } = null!;
    public OrderSide Side { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public DateTimeOffset ExecutedAt { get; set; }
    public decimal Commission { get; set; }

    public Guid? TradeId { get; set; }
    public Trade? Trade { get; set; }
}
