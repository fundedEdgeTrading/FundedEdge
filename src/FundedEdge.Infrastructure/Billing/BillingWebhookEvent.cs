namespace FundedEdge.Infrastructure.Billing;

/// <summary>
/// Proyección mínima de un evento de webhook de Stripe, ya verificado, con solo los campos que
/// BillingWebhookProcessor necesita. Mantenerlo como DTO plano (sin tipos del SDK de Stripe)
/// permite testear el procesador sin depender de Stripe.net ni de JSON real firmado —
/// ver GUIA_MONETIZACION_Y_MARKETING.md §6 (F4).
/// </summary>
public sealed record BillingWebhookEvent(
    string Type,
    string? ClientReferenceId,
    string? CustomerId,
    string? PlanTierMetadata,
    string? SubscriptionStatus,
    string? EventId = null);
