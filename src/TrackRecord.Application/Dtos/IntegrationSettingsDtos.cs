namespace TrackRecord.Application.Dtos;

public record TradovateSettingsDto(bool IsConfigured, string? Name, int? Cid, string Source);

public record SaveTradovateSettingsRequest(string Name, string Password, int Cid, string Sec, string? DeviceId);

public record IngestSettingsDto(bool IsConfigured, string Source);

public record AiSettingsStatusDto(bool IsConfigured);
