using TrackRecord.Domain.Enums;

namespace TrackRecord.Application.Dtos;

/// <summary>Fuente monitorizada de reglas de una firma, con el estado de la última comprobación.</summary>
public record RuleSourceDto(
    Guid Id,
    Guid PropFirmId,
    string PropFirmName,
    string Url,
    RuleSourceKind Kind,
    bool IsEnabled,
    DateTimeOffset? LastCheckedAt,
    DateTimeOffset? LastChangedAt,
    string? LastError);

public record UpsertRuleSourceRequest(
    Guid PropFirmId,
    string Url,
    RuleSourceKind Kind,
    bool IsEnabled);

/// <summary>
/// Resultado de comprobar una fuente: si su contenido cambió desde el último hash y, cuando la
/// extracción LLM corre (cambio detectado o forzada), cuántas propuestas dejó pendientes.
/// </summary>
public record RuleSourceCheckResult(bool Changed, string? Error, int ProposalsCreated = 0, string? ExtractionError = null);
