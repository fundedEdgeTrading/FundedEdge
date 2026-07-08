using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TrackRecord.Domain.Entities;
using TrackRecord.Domain.Enums;
using TrackRecord.Infrastructure.Persistence;

namespace TrackRecord.Infrastructure.Billing;

/// <summary>
/// Aplica los efectos de un evento de webhook de Stripe ya verificado (firma comprobada por el
/// endpoint) sobre el plan del usuario. Ver GUIA_MONETIZACION_Y_MARKETING.md §6 (F4).
/// </summary>
public class BillingWebhookProcessor(IDbContextFactory<TrackRecordDbContext> dbFactory, ILogger<BillingWebhookProcessor> logger)
{
    public const string CheckoutSessionCompleted = "checkout.session.completed";
    public const string SubscriptionUpdated = "customer.subscription.updated";
    public const string SubscriptionDeleted = "customer.subscription.deleted";

    private static readonly HashSet<string> CanceledSubscriptionStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "canceled", "unpaid", "incomplete_expired" };

    public async Task ApplyAsync(BillingWebhookEvent evt, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Idempotencia [SEC-16]: si el proveedor reenvía un evento ya procesado, se ignora en vez
        // de reaplicarlo. La firma verificada + la tolerancia temporal del SDK ya limitan el replay
        // malicioso; esto cubre los reenvíos legítimos de Stripe.
        if (evt.EventId is not null &&
            await db.ProcessedWebhookEvents.AnyAsync(e => e.Id == evt.EventId, ct))
        {
            logger.LogInformation("Evento de webhook {EventId} ya procesado; se ignora.", evt.EventId);
            return;
        }

        switch (evt.Type)
        {
            case CheckoutSessionCompleted:
                await ApplyCheckoutCompletedAsync(db, evt, ct);
                break;
            case SubscriptionDeleted:
                await DowngradeByCustomerIdAsync(db, evt.CustomerId, ct);
                break;
            case SubscriptionUpdated when evt.SubscriptionStatus is not null && CanceledSubscriptionStatuses.Contains(evt.SubscriptionStatus):
                await DowngradeByCustomerIdAsync(db, evt.CustomerId, ct);
                break;
        }

        // Marca el evento como procesado (también los tipos no manejados, para no reprocesarlos).
        if (evt.EventId is not null)
        {
            db.ProcessedWebhookEvents.Add(new ProcessedWebhookEvent
            {
                Id = evt.EventId,
                ProcessedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task ApplyCheckoutCompletedAsync(TrackRecordDbContext db, BillingWebhookEvent evt, CancellationToken ct)
    {
        if (evt.ClientReferenceId is null || evt.PlanTierMetadata is null || !Enum.TryParse<PlanTier>(evt.PlanTierMetadata, out var tier))
        {
            logger.LogWarning("checkout.session.completed sin ClientReferenceId o metadata de plan válida; se ignora.");
            return;
        }

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == evt.ClientReferenceId, ct);
        if (user is null)
        {
            logger.LogWarning("checkout.session.completed para un usuario inexistente ({UserId}); se ignora.", evt.ClientReferenceId);
            return;
        }

        user.PlanTier = tier;
        user.StripeCustomerId = evt.CustomerId;
        await db.SaveChangesAsync(ct);
    }

    private async Task DowngradeByCustomerIdAsync(TrackRecordDbContext db, string? customerId, CancellationToken ct)
    {
        if (customerId is null) return;

        var user = await db.Users.SingleOrDefaultAsync(u => u.StripeCustomerId == customerId, ct);
        if (user is null) return;

        user.PlanTier = PlanTier.Starter;
        await db.SaveChangesAsync(ct);
    }
}
