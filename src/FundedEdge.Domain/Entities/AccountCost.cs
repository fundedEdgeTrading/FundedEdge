using FundedEdge.Domain.Common;
using FundedEdge.Domain.Enums;

namespace FundedEdge.Domain.Entities;

public class AccountCost : Entity
{
    public Guid AccountId { get; set; }
    public TradingAccount? Account { get; set; }

    public CostKind Kind { get; set; }
    public decimal Amount { get; set; }
    public DateOnly PaidOn { get; set; }
    public string? Notes { get; set; }
}
