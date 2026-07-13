using FundedEdge.Domain.Common;
using FundedEdge.Domain.Enums;

namespace FundedEdge.Domain.Entities;

public class TradingAccount : Entity
{
    /// <summary>
    /// Dueño de la cuenta (Id de ApplicationUser). Nullable a nivel de columna solo para permitir
    /// el backfill de datos sembrados/de prueba anteriores a la autenticación — ver
    /// UserBackfillService. Toda cuenta creada desde que existe login siempre lo trae informado.
    /// </summary>
    public string? UserId { get; set; }

    public Guid PropFirmId { get; set; }
    public PropFirm? PropFirm { get; set; }

    /// <summary>
    /// Programa de evaluación del catálogo al que pertenece esta cuenta. Nullable para
    /// compatibilidad con cuentas creadas antes de F6 (flujo manual sin selección de programa).
    /// Cuando está informado, permite cruzar los datos de operativa con las reglas del programa.
    /// </summary>
    public Guid? EvaluationProgramId { get; set; }
    public EvaluationProgram? EvaluationProgram { get; set; }

    public string DisplayName { get; set; } = null!;
    public string? ExternalAccountId { get; set; }

    public decimal AccountSize { get; set; }
    public decimal ProfitTarget { get; set; }
    public decimal MaxDrawdown { get; set; }
    public DrawdownType DrawdownType { get; set; }

    public AccountStage Stage { get; set; } = AccountStage.Evaluation;
    public DataFeedType Feed { get; set; } = DataFeedType.Manual;

    public DateOnly PurchasedOn { get; set; }
    public DateOnly? FundedOn { get; set; }
    public DateOnly? ClosedOn { get; set; }

    public string? Notes { get; set; }

    public List<AccountEvent> Events { get; set; } = [];
    public List<Trade> Trades { get; set; } = [];
    public List<Payout> Payouts { get; set; } = [];
    public List<AccountCost> Costs { get; set; } = [];

    public bool IsTerminal =>
        Stage is AccountStage.Failed or AccountStage.Withdrawn or AccountStage.Expired;

    /// <summary>
    /// Cambia la etapa de la cuenta dejando constancia auditable del cambio.
    /// Ajusta automáticamente FundedOn/ClosedOn según la transición.
    /// </summary>
    public AccountEvent TransitionTo(AccountStage newStage, DateTimeOffset occurredAt, string? notes = null)
    {
        var evt = new AccountEvent
        {
            AccountId = Id,
            FromStage = Stage,
            ToStage = newStage,
            OccurredAt = occurredAt,
            Notes = notes,
        };

        if (newStage == AccountStage.Funded && FundedOn is null)
        {
            FundedOn = DateOnly.FromDateTime(occurredAt.Date);
        }

        if (newStage is AccountStage.Failed or AccountStage.Withdrawn or AccountStage.Expired)
        {
            ClosedOn = DateOnly.FromDateTime(occurredAt.Date);
        }

        Stage = newStage;
        Events.Add(evt);
        return evt;
    }
}
