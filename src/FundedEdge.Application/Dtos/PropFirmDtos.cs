using FundedEdge.Domain.Enums;

namespace FundedEdge.Application.Dtos;

public record PropFirmDto(
    Guid Id,
    string Name,
    string? Website,
    string? Notes,
    int? MinDaysBetweenPayouts,
    int AccountCount,
    FirmHealthStatus HealthStatus,
    string? Country,
    string? HealthNotes,
    DateOnly? HealthUpdatedOn,
    string? RulesMarkdown = null,
    string? RulesSourceUrls = null,
    string? RulesSource = null,
    DateOnly? RulesUpdatedOn = null);

public record UpsertPropFirmRequest(
    string Name,
    string? Website,
    string? Notes,
    int? MinDaysBetweenPayouts,
    FirmHealthStatus HealthStatus = FirmHealthStatus.Active,
    string? Country = null,
    string? HealthNotes = null);

/// <summary>
/// Tiempo real de pago de una firma, agregado anónimo de los payouts cobrados por TODOS los
/// usuarios de la instancia (M6, capa comunidad): días entre solicitar y cobrar. Solo se publica
/// con muestra suficiente (ver umbrales en PropFirmService) para no exponer datos individuales.
/// </summary>
public record FirmPayoutSpeedDto(Guid FirmId, int PayoutCount, int TraderCount, int MedianDays, int P90Days);
