using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using TrackRecord.Application.Abstractions;
using TrackRecord.Infrastructure.Billing;
using TrackRecord.Infrastructure.Persistence;
using PlanTier = TrackRecord.Domain.Enums.PlanTier;

namespace TrackRecord.Web.Endpoints;

/// <summary>
/// Checkout/portal/webhook de Stripe (ver GUIA_MONETIZACION_Y_MARKETING.md §6, F4). El mapeo
/// price→tier y la lógica de aplicar el webhook viven en TrackRecord.Infrastructure.Billing
/// (testeables sin el SDK de Stripe); aquí solo está el "pegamento" que sí depende del SDK:
/// crear las sesiones de Stripe y verificar/parsear la firma del webhook.
/// </summary>
public static class BillingEndpoints
{
    public static IEndpointRouteBuilder MapBillingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/billing");

        group.MapPost("/checkout", HandleCheckoutAsync).RequireAuthorization();
        group.MapPost("/portal", HandlePortalAsync).RequireAuthorization();
        group.MapPost("/webhook", HandleWebhookAsync).AllowAnonymous();

        return app;
    }

    /// <summary>
    /// URL base absoluta para construir los retornos de Stripe. Prioriza App:BaseUrl (configuración
    /// de confianza) sobre el Host de la petición, que es manipulable si AllowedHosts no está
    /// restringido — evita el envenenamiento de las URLs de éxito/cancelación. [SEC-09]
    /// </summary>
    private static string ResolveBaseUrl(HttpContext context, IConfiguration configuration)
    {
        var configured = configuration["App:BaseUrl"];
        return !string.IsNullOrWhiteSpace(configured)
            ? configured.TrimEnd('/')
            : $"{context.Request.Scheme}://{context.Request.Host}";
    }

    private static async Task<IResult> HandleCheckoutAsync(
        HttpContext context,
        [FromForm] PlanTier tier,
        [FromForm] bool yearly,
        BillingOptions options,
        ICurrentUserAccessor currentUser,
        IConfiguration configuration,
        CancellationToken ct)
    {
        if (!options.IsConfigured)
        {
            return Results.BadRequest("Los pagos no están configurados todavía.");
        }

        var priceId = options.Prices.GetPriceId(tier, yearly);
        if (priceId is null)
        {
            return Results.BadRequest("Ese plan no está disponible ahora mismo.");
        }

        var userId = await currentUser.RequireUserIdAsync();
        var baseUrl = ResolveBaseUrl(context, configuration);

        var sessionService = new SessionService();
        var session = await sessionService.CreateAsync(new SessionCreateOptions
        {
            Mode = "subscription",
            LineItems = [new SessionLineItemOptions { Price = priceId, Quantity = 1 }],
            ClientReferenceId = userId,
            Metadata = new Dictionary<string, string> { ["planTier"] = tier.ToString() },
            SuccessUrl = $"{baseUrl}/plan?ok=1",
            CancelUrl = $"{baseUrl}/plan",
        }, cancellationToken: ct);

        return Results.Redirect(session.Url);
    }

    private static async Task<IResult> HandlePortalAsync(
        HttpContext context,
        BillingOptions options,
        IDbContextFactory<TrackRecordDbContext> dbFactory,
        ICurrentUserAccessor currentUser,
        IConfiguration configuration,
        CancellationToken ct)
    {
        if (!options.IsConfigured)
        {
            return Results.BadRequest("Los pagos no están configurados todavía.");
        }

        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var customerId = await db.Users.Where(u => u.Id == userId).Select(u => u.StripeCustomerId).SingleOrDefaultAsync(ct);
        if (customerId is null)
        {
            return Results.BadRequest("Todavía no tienes una suscripción de pago.");
        }

        var baseUrl = ResolveBaseUrl(context, configuration);
        var portalService = new Stripe.BillingPortal.SessionService();
        var portalSession = await portalService.CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = customerId,
            ReturnUrl = $"{baseUrl}/plan",
        }, cancellationToken: ct);

        return Results.Redirect(portalSession.Url);
    }

    private static async Task<IResult> HandleWebhookAsync(
        HttpRequest request,
        BillingOptions options,
        BillingWebhookProcessor processor,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        if (!options.IsConfigured)
        {
            return Results.BadRequest();
        }

        using var reader = new StreamReader(request.Body);
        var json = await reader.ReadToEndAsync(ct);

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, request.Headers["Stripe-Signature"], options.WebhookSecret);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Webhook de Stripe con firma inválida.");
            return Results.BadRequest();
        }

        await processor.ApplyAsync(ToBillingEvent(stripeEvent), ct);
        return Results.Ok();
    }

    private static BillingWebhookEvent ToBillingEvent(Event stripeEvent)
    {
        if (stripeEvent.Data.Object is Session session)
        {
            var planTier = session.Metadata is not null && session.Metadata.TryGetValue("planTier", out var t) ? t : null;
            return new BillingWebhookEvent(stripeEvent.Type, session.ClientReferenceId, session.CustomerId, planTier, null, stripeEvent.Id);
        }

        if (stripeEvent.Data.Object is Subscription subscription)
        {
            return new BillingWebhookEvent(stripeEvent.Type, null, subscription.CustomerId, null, subscription.Status, stripeEvent.Id);
        }

        return new BillingWebhookEvent(stripeEvent.Type, null, null, null, null, stripeEvent.Id);
    }
}
