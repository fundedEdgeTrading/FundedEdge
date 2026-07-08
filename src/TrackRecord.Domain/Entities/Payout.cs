using TrackRecord.Domain.Common;
using TrackRecord.Domain.Enums;

namespace TrackRecord.Domain.Entities;

public class Payout : Entity
{
    public Guid AccountId { get; set; }
    public TradingAccount? Account { get; set; }

    public decimal AmountRequested { get; set; }
    public decimal AmountReceived { get; set; }
    public DateOnly RequestedOn { get; set; }
    public DateOnly? PaidOn { get; set; }
    public PayoutStatus Status { get; set; } = PayoutStatus.Requested;
    public string? Notes { get; set; }
}
