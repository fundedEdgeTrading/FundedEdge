using FundedEdge.Domain.Common;

namespace FundedEdge.Domain.Entities;

/// <summary>
/// Setup de entrada definido por el propio usuario (p.ej. "Breakout apertura", "Pullback a
/// EMA20") para poder etiquetar sus trades por técnica utilizada y comparar rendimiento entre
/// setups — algo que ningún export de CSV (Tradovate/NT8) trae de por sí.
/// </summary>
public class TradeSetup : Entity
{
    public string UserId { get; set; } = null!;
    public string Name { get; set; } = null!;
}
