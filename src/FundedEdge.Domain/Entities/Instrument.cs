using FundedEdge.Domain.Common;

namespace FundedEdge.Domain.Entities;

public class Instrument : Entity
{
    public string Root { get; set; } = null!;    // "ES", "NQ", "MNQ", "GC"...
    public string Name { get; set; } = null!;
    public decimal TickSize { get; set; }
    public decimal TickValue { get; set; }
}
