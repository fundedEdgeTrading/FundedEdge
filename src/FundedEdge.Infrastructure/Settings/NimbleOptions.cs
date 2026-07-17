namespace FundedEdge.Infrastructure.Settings;

/// <summary>
/// Configuración del cliente de recuperación web Nimble (Web API REST). La credencial nunca va en
/// appsettings.json versionado: se define por entorno o User Secrets (clave "Nimble:ApiKey" o la
/// variable de entorno NIMBLE_API_KEY). <see cref="ApiKey"/> es la cadena base64 de credenciales
/// que genera el panel de Nimble y que viaja en la cabecera Authorization: Basic.
/// </summary>
public sealed record NimbleOptions(bool IsConfigured, string? ApiKey, string BaseUrl)
{
    /// <summary>Endpoint real-time por defecto. Sobrescribible con "Nimble:BaseUrl".</summary>
    public const string DefaultBaseUrl = "https://api.webit.live/api/v1/";
}
