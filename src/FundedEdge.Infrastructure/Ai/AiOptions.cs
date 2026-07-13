namespace FundedEdge.Infrastructure.Ai;

/// <summary>
/// Indica si hay credenciales de Anthropic disponibles (ANTHROPIC_API_KEY o Ai:ApiKey en
/// configuración), calculado una vez en el arranque para poder informar en la UI sin
/// exponer la clave.
/// </summary>
public record AiOptions(bool IsApiKeyConfigured);
