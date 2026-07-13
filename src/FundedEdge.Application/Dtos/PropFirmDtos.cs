namespace FundedEdge.Application.Dtos;

public record PropFirmDto(Guid Id, string Name, string? Website, string? Notes, int? MinDaysBetweenPayouts, int AccountCount);

public record UpsertPropFirmRequest(string Name, string? Website, string? Notes, int? MinDaysBetweenPayouts);
