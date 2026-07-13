namespace FundedEdge.Application.Dtos;

/// <summary>Semáforo de cumplimiento de reglas (GUIA_FUNCIONALIDADES_PROPUESTAS.md §2.2/§3.5).</summary>
public enum ComplianceLevel { Green, Yellow, Red }

/// <summary>
/// Estado en vivo de una cuenta frente a las reglas de su programa: pérdida diaria, drawdown y
/// consistencia. Null en un campo de regla = la firma no la impone (no aplica, no es un fallo).
/// </summary>
public record AccountComplianceStatusDto(
    Guid AccountId,
    string AccountDisplayName,
    decimal? DailyLossLimit,
    decimal DailyLossUsedToday,
    decimal? DailyLossRemaining,
    ComplianceLevel DailyLossLevel,
    decimal DrawdownLimit,
    decimal DrawdownRemainingBuffer,
    ComplianceLevel DrawdownLevel,
    decimal? ConsistencyMaxDayFraction,
    double? ConsistencyTopDayFraction,
    ComplianceLevel? ConsistencyLevel,
    ComplianceLevel OverallLevel);
