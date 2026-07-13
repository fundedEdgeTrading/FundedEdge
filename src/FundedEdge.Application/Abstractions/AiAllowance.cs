namespace FundedEdge.Application.Abstractions;

/// <summary>Cupo de IA restante del usuario actual para la ventana/mes en curso.</summary>
public sealed record AiAllowance(
    bool CanGenerateReport,
    bool CanAskQuestion,
    int ReportsUsed,
    int ReportsLimit,
    int QuestionsUsed,
    int? QuestionsLimit,
    DateTimeOffset WindowResetsAt);
