using TrackRecord.Domain.Common;
using TrackRecord.Domain.Enums;

namespace TrackRecord.Domain.Entities;

/// <summary>
/// Historial auditable de transiciones de una cuenta (compra, paso a fondeada, reset, fallo...).
/// </summary>
public class AccountEvent : Entity
{
    public Guid AccountId { get; set; }
    public TradingAccount? Account { get; set; }

    public AccountStage FromStage { get; set; }
    public AccountStage ToStage { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string? Notes { get; set; }
}
