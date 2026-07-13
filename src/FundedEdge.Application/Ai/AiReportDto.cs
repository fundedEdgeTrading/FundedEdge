using FundedEdge.Domain.Enums;

namespace FundedEdge.Application.Ai;

public record AiReportDto(
    Guid Id,
    AiReportKind Kind,
    string? Question,
    string Content,
    DateTimeOffset CreatedAt,
    string Model,
    int InputTokens,
    int OutputTokens);
