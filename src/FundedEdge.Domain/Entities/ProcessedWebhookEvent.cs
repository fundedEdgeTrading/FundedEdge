namespace FundedEdge.Domain.Entities;

/// <summary>
/// Registro de idempotencia para los webhooks de facturación ya procesados. La clave primaria es
/// el identificador del evento del proveedor (p.ej. el "evt_..." de Stripe): antes de aplicar un
/// evento se comprueba que no exista aquí, evitando reaplicar reenvíos legítimos del proveedor.
/// Ver GUIA_SEGURIDAD.md (SEC-16).
/// </summary>
public class ProcessedWebhookEvent
{
    /// <summary>Id del evento del proveedor de pagos (único).</summary>
    public string Id { get; set; } = null!;

    public DateTimeOffset ProcessedAt { get; set; }
}
