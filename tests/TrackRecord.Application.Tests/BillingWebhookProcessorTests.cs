using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TrackRecord.Domain.Enums;
using TrackRecord.Infrastructure.Billing;
using TrackRecord.Infrastructure.Identity;

namespace TrackRecord.Application.Tests;

public class BillingWebhookProcessorTests
{
    private const string UserId = "user-1";

    private static async Task<InMemoryDbContextFactory> SeedUserAsync(PlanTier tier = PlanTier.Starter, string? stripeCustomerId = null)
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        await using var db = await factory.CreateDbContextAsync();
        db.Users.Add(new ApplicationUser { Id = UserId, UserName = "user@test.com", PlanTier = tier, StripeCustomerId = stripeCustomerId });
        await db.SaveChangesAsync();
        return factory;
    }

    private static BillingWebhookProcessor BuildProcessor(InMemoryDbContextFactory factory) =>
        new(factory, NullLogger<BillingWebhookProcessor>.Instance);

    private static async Task<ApplicationUser> ReloadUserAsync(InMemoryDbContextFactory factory)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Users.SingleAsync(u => u.Id == UserId);
    }

    [Fact]
    public async Task CheckoutSessionCompleted_UpgradesUserToTheMetadataTier()
    {
        var factory = await SeedUserAsync();
        var evt = new BillingWebhookEvent(BillingWebhookProcessor.CheckoutSessionCompleted, UserId, "cus_123", "Pro", null);

        await BuildProcessor(factory).ApplyAsync(evt);

        var user = await ReloadUserAsync(factory);
        Assert.Equal(PlanTier.Pro, user.PlanTier);
        Assert.Equal("cus_123", user.StripeCustomerId);
    }

    [Fact]
    public async Task CheckoutSessionCompleted_UnknownUser_DoesNotThrow()
    {
        var factory = await SeedUserAsync();
        var evt = new BillingWebhookEvent(BillingWebhookProcessor.CheckoutSessionCompleted, "no-such-user", "cus_123", "Pro", null);

        await BuildProcessor(factory).ApplyAsync(evt); // no debe lanzar

        var user = await ReloadUserAsync(factory);
        Assert.Equal(PlanTier.Starter, user.PlanTier); // sin cambios
    }

    [Fact]
    public async Task CheckoutSessionCompleted_MissingPlanTierMetadata_IsIgnored()
    {
        var factory = await SeedUserAsync();
        var evt = new BillingWebhookEvent(BillingWebhookProcessor.CheckoutSessionCompleted, UserId, "cus_123", PlanTierMetadata: null, null);

        await BuildProcessor(factory).ApplyAsync(evt);

        var user = await ReloadUserAsync(factory);
        Assert.Equal(PlanTier.Starter, user.PlanTier);
        Assert.Null(user.StripeCustomerId);
    }

    [Fact]
    public async Task SubscriptionDeleted_DowngradesTheMatchingCustomerToStarter()
    {
        var factory = await SeedUserAsync(PlanTier.Elite, stripeCustomerId: "cus_123");
        var evt = new BillingWebhookEvent(BillingWebhookProcessor.SubscriptionDeleted, null, "cus_123", null, null);

        await BuildProcessor(factory).ApplyAsync(evt);

        var user = await ReloadUserAsync(factory);
        Assert.Equal(PlanTier.Starter, user.PlanTier);
    }

    [Theory]
    [InlineData("canceled")]
    [InlineData("unpaid")]
    [InlineData("incomplete_expired")]
    public async Task SubscriptionUpdated_TerminalStatus_DowngradesToStarter(string status)
    {
        var factory = await SeedUserAsync(PlanTier.Pro, stripeCustomerId: "cus_123");
        var evt = new BillingWebhookEvent(BillingWebhookProcessor.SubscriptionUpdated, null, "cus_123", null, status);

        await BuildProcessor(factory).ApplyAsync(evt);

        var user = await ReloadUserAsync(factory);
        Assert.Equal(PlanTier.Starter, user.PlanTier);
    }

    [Fact]
    public async Task SubscriptionUpdated_ActiveStatus_DoesNotDowngrade()
    {
        var factory = await SeedUserAsync(PlanTier.Pro, stripeCustomerId: "cus_123");
        var evt = new BillingWebhookEvent(BillingWebhookProcessor.SubscriptionUpdated, null, "cus_123", null, "active");

        await BuildProcessor(factory).ApplyAsync(evt);

        var user = await ReloadUserAsync(factory);
        Assert.Equal(PlanTier.Pro, user.PlanTier);
    }

    [Fact]
    public async Task SubscriptionDeleted_UnknownCustomerId_DoesNotThrow()
    {
        var factory = await SeedUserAsync(PlanTier.Elite, stripeCustomerId: "cus_123");
        var evt = new BillingWebhookEvent(BillingWebhookProcessor.SubscriptionDeleted, null, "cus_other", null, null);

        await BuildProcessor(factory).ApplyAsync(evt);

        var user = await ReloadUserAsync(factory);
        Assert.Equal(PlanTier.Elite, user.PlanTier); // no coincide, no se toca
    }

    [Fact]
    public async Task UnknownEventType_IsIgnored()
    {
        var factory = await SeedUserAsync(PlanTier.Pro, stripeCustomerId: "cus_123");
        var evt = new BillingWebhookEvent("customer.created", null, "cus_123", null, null);

        await BuildProcessor(factory).ApplyAsync(evt);

        var user = await ReloadUserAsync(factory);
        Assert.Equal(PlanTier.Pro, user.PlanTier);
    }

    [Fact]
    public async Task DuplicateEventId_IsProcessedOnlyOnce()
    {
        var factory = await SeedUserAsync();
        var evt = new BillingWebhookEvent(BillingWebhookProcessor.CheckoutSessionCompleted, UserId, "cus_123", "Pro", null, "evt_dup");
        var processor = BuildProcessor(factory);

        await processor.ApplyAsync(evt);
        Assert.Equal(PlanTier.Pro, (await ReloadUserAsync(factory)).PlanTier);

        // Simula un cambio posterior y reenvía el MISMO evento: debe ignorarse (idempotencia).
        await using (var db = await factory.CreateDbContextAsync())
        {
            var u = await db.Users.SingleAsync(x => x.Id == UserId);
            u.PlanTier = PlanTier.Starter;
            await db.SaveChangesAsync();
        }

        await processor.ApplyAsync(evt);
        Assert.Equal(PlanTier.Starter, (await ReloadUserAsync(factory)).PlanTier); // no se reaplicó
    }

    [Fact]
    public async Task ProcessedEvent_IsRecordedForIdempotency()
    {
        var factory = await SeedUserAsync();
        var evt = new BillingWebhookEvent(BillingWebhookProcessor.CheckoutSessionCompleted, UserId, "cus_123", "Pro", null, "evt_42");

        await BuildProcessor(factory).ApplyAsync(evt);

        await using var db = await factory.CreateDbContextAsync();
        Assert.True(await db.ProcessedWebhookEvents.AnyAsync(e => e.Id == "evt_42"));
    }
}
