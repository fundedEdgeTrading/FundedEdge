using TrackRecord.Domain.Common;
using TrackRecord.Domain.Enums;

namespace TrackRecord.Domain.Entities;

public class AccountCost : Entity
{
    public Guid AccountId { get; set; }
    public TradingAccount? Account { get; set; }

    public CostKind Kind { get; set; }
    public decimal Amount { get; set; }
    public DateOnly PaidOn { get; set; }
    public string? Notes { get; set; }
}
