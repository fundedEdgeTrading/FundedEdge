using FundedEdge.Domain.Enums;

namespace FundedEdge.Application.Dtos;

public record TradingAccountListItemDto(
    Guid Id,
    string DisplayName,
    Guid PropFirmId,
    string PropFirmName,
    decimal AccountSize,
    decimal ProfitTarget,
    AccountStage Stage,
    DataFeedType Feed,
    string? ExternalAccountId,
    DateOnly PurchasedOn,
    DateOnly? FundedOn,
    DateOnly? ClosedOn,
    decimal NetPnL,
    decimal TotalCosts,
    decimal TotalPayoutsReceived,
    // Objetivo de profit efectivo (target base elevado por la regla de consistencia, si aplica).
    decimal EffectiveProfitTarget);

public record TradingAccountDetailDto(
    Guid Id,
    Guid PropFirmId,
    string PropFirmName,
    string DisplayName,
    string? ExternalAccountId,
    decimal AccountSize,
    decimal ProfitTarget,
    decimal MaxDrawdown,
    DrawdownType DrawdownType,
    AccountStage Stage,
    DataFeedType Feed,
    DateOnly PurchasedOn,
    DateOnly? FundedOn,
    DateOnly? ClosedOn,
    string? Notes,
    IReadOnlyList<AccountEventDto> Events,
    IReadOnlyList<AccountCostDto> Costs,
    IReadOnlyList<PayoutDto> Payouts,
    IReadOnlyList<TradeListItemDto> Trades,
    // Solo si la firma tiene MinDaysBetweenPayouts configurado y la cuenta está fondeada.
    DateOnly? NextPayoutEligibleOn,
    // Programa del catálogo vinculado a esta cuenta (null si se creó con flujo manual).
    Guid? EvaluationProgramId,
    // Objetivo de profit efectivo (target base elevado por la regla de consistencia, si aplica).
    decimal EffectiveProfitTarget);

public record AccountEventDto(Guid Id, AccountStage FromStage, AccountStage ToStage, DateTimeOffset OccurredAt, string? Notes);

public record AccountCostDto(Guid Id, CostKind Kind, decimal Amount, DateOnly PaidOn, string? Notes);

public record PayoutDto(Guid Id, decimal AmountRequested, decimal AmountReceived, DateOnly RequestedOn, DateOnly? PaidOn, PayoutStatus Status, string? Notes);

public record CreateTradingAccountRequest(
    Guid PropFirmId,
    string DisplayName,
    string? ExternalAccountId,
    decimal AccountSize,
    decimal ProfitTarget,
    decimal MaxDrawdown,
    DrawdownType DrawdownType,
    DataFeedType Feed,
    DateOnly PurchasedOn,
    decimal? EvaluationCost,
    string? Notes,
    // Programa del catálogo seleccionado en el formulario en cascada (null = flujo manual).
    Guid? EvaluationProgramId = null);

public record TransitionAccountStageRequest(Guid AccountId, AccountStage NewStage, DateTimeOffset OccurredAt, string? Notes);

public record UpdateAccountConnectionRequest(Guid AccountId, DataFeedType Feed, string? ExternalAccountId);

public record RenameAccountRequest(Guid AccountId, string DisplayName);

public record AddAccountCostRequest(Guid AccountId, CostKind Kind, decimal Amount, DateOnly PaidOn, string? Notes);

public record AddPayoutRequest(Guid AccountId, decimal AmountRequested, decimal AmountReceived, DateOnly RequestedOn, DateOnly? PaidOn, PayoutStatus Status, string? Notes);
