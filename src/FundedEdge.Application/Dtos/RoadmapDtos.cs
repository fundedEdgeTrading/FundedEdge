using FundedEdge.Domain.Enums;

namespace FundedEdge.Application.Dtos;

public record RoadmapAccountItemDto(
    Guid Id,
    string DisplayName,
    string PropFirmName,
    AccountStage Stage,
    decimal AvailableToWithdraw,
    decimal ProgressPercent);

public record ReinvestmentPlanDto(
    decimal TotalPayoutsReceived,
    decimal ReinvestPercent,
    decimal PersonalPercent,
    decimal TaxPercent,
    decimal SuggestedReinvestment,
    decimal SuggestedPersonal,
    decimal SuggestedTaxReserve);

public record RoadmapDto(
    IReadOnlyList<RoadmapAccountItemDto> FundedPriority,
    IReadOnlyList<RoadmapAccountItemDto> EvaluationPriority,
    ReinvestmentPlanDto Reinvestment);
