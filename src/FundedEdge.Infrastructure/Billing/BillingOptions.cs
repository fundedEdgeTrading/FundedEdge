namespace FundedEdge.Infrastructure.Billing;

/// <summary>
/// Indica si hay credenciales de Stripe configuradas (calculado una vez en el arranque) y
/// expone el catálogo de precios. Sin esto configurado, /plan muestra los botones de upgrade
/// deshabilitados con "Pagos no configurados" — la app sigue funcionando igual.
/// </summary>
public sealed record BillingOptions(bool IsConfigured, string? WebhookSecret, StripePriceCatalog Prices);
