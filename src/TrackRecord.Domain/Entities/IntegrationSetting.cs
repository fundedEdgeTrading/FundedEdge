namespace TrackRecord.Domain.Entities;

/// <summary>
/// Par clave/valor cifrado para credenciales de integraciones configuradas desde /settings
/// (p.ej. "Tradovate:Name", "Ingest:NinjaTraderApiKey"). El valor se cifra con IDataProtector
/// antes de persistirse; ver DataProtectedIntegrationSettingsStore. No se usa para la clave de
/// Anthropic, que solo se lee de User Secrets/entorno (ver README).
/// </summary>
public class IntegrationSetting
{
    public string Key { get; set; } = null!;
    public string ProtectedValue { get; set; } = null!;
    public DateTimeOffset UpdatedAt { get; set; }
}
