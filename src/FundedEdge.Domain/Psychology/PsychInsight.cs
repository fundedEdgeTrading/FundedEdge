namespace FundedEdge.Domain.Psychology;

public enum InsightSeverity { Info, Warning, Critical }

/// <summary>
/// Diagnóstico emitido por un detector determinista (GUIA_PSICOLOGIA_TRADING.md §6.1), con los
/// trades concretos como evidencia — nunca una afirmación sin datos detrás.
/// </summary>
public record PsychInsight(
    string DetectorKey,
    InsightSeverity Severity,
    string Title,
    string Recommendation,
    IReadOnlyList<Guid> EvidenceTradeIds);
