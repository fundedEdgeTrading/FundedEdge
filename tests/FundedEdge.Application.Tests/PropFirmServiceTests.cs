using Microsoft.Extensions.Logging.Abstractions;
using FundedEdge.Application.Dtos;
using FundedEdge.Domain.Entities;
using FundedEdge.Domain.Enums;
using FundedEdge.Infrastructure.Email;
using FundedEdge.Infrastructure.Persistence;
using FundedEdge.Infrastructure.Services;

namespace FundedEdge.Application.Tests;

public class PropFirmServiceTests
{
    private sealed class RecordingEmailSender : IAppEmailSender
    {
        public List<(string To, string Subject)> Sent { get; } = [];

        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
        {
            Sent.Add((to, subject));
            return Task.CompletedTask;
        }
    }

    private static async Task<InMemoryDbContextFactory> SeedAsync(Action<FundedEdgeDbContext> seed)
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        await using var db = await factory.CreateDbContextAsync();
        seed(db);
        await db.SaveChangesAsync();
        return factory;
    }

    private static PropFirmService BuildService(InMemoryDbContextFactory factory, RecordingEmailSender? sender = null) =>
        new(factory, sender ?? new RecordingEmailSender(), NullLogger<PropFirmService>.Instance);

    private static TradingAccount Account(PropFirm firm, string userId, AccountStage stage = AccountStage.Funded) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        PropFirm = firm,
        PropFirmId = firm.Id,
        DisplayName = $"{firm.Name} {userId}",
        AccountSize = 50_000m,
        ProfitTarget = 3_000m,
        MaxDrawdown = 2_500m,
        PurchasedOn = new DateOnly(2026, 1, 1),
        Stage = stage,
    };

    private static Payout PaidPayout(TradingAccount account, DateOnly requested, int daysToPay) => new()
    {
        AccountId = account.Id,
        AmountRequested = 500m,
        AmountReceived = 500m,
        RequestedOn = requested,
        PaidOn = requested.AddDays(daysToPay),
        Status = PayoutStatus.Paid,
    };

    [Fact]
    public async Task GetPayoutSpeed_BelowMinimumSample_IsNotPublished()
    {
        var firm = new PropFirm { Id = Guid.NewGuid(), Name = "Apex" };
        var factory = await SeedAsync(db =>
        {
            var a1 = Account(firm, "u1");
            var a2 = Account(firm, "u2");
            db.TradingAccounts.AddRange(a1, a2);
            // 5 payouts pero solo 2 traders → por debajo de MinTraderSample.
            db.Payouts.AddRange(
                PaidPayout(a1, new DateOnly(2026, 1, 5), 2),
                PaidPayout(a1, new DateOnly(2026, 2, 5), 3),
                PaidPayout(a1, new DateOnly(2026, 3, 5), 4),
                PaidPayout(a2, new DateOnly(2026, 1, 5), 2),
                PaidPayout(a2, new DateOnly(2026, 2, 5), 3));
        });

        var speed = await BuildService(factory).GetPayoutSpeedAsync();

        Assert.Empty(speed);
    }

    [Fact]
    public async Task GetPayoutSpeed_ComputesMedianAndP90AcrossUsers()
    {
        var firm = new PropFirm { Id = Guid.NewGuid(), Name = "Tradeify" };
        var factory = await SeedAsync(db =>
        {
            var a1 = Account(firm, "u1");
            var a2 = Account(firm, "u2");
            var a3 = Account(firm, "u3");
            db.TradingAccounts.AddRange(a1, a2, a3);
            // Días hasta cobrar: 1, 2, 3, 4, 10 → mediana 3, P90 10 (rango más cercano).
            db.Payouts.AddRange(
                PaidPayout(a1, new DateOnly(2026, 1, 5), 1),
                PaidPayout(a1, new DateOnly(2026, 2, 5), 2),
                PaidPayout(a2, new DateOnly(2026, 1, 5), 3),
                PaidPayout(a2, new DateOnly(2026, 2, 5), 4),
                PaidPayout(a3, new DateOnly(2026, 1, 5), 10));
        });

        var speed = await BuildService(factory).GetPayoutSpeedAsync();

        Assert.True(speed.TryGetValue(firm.Id, out var stats));
        Assert.Equal(5, stats!.PayoutCount);
        Assert.Equal(3, stats.TraderCount);
        Assert.Equal(3, stats.MedianDays);
        Assert.Equal(10, stats.P90Days);
    }

    [Fact]
    public async Task Update_HealthStatusChange_EmailsUsersWithActiveAccountsOnce()
    {
        var firm = new PropFirm { Id = Guid.NewGuid(), Name = "Lucid", HealthStatus = FirmHealthStatus.Active };
        var factory = await SeedAsync(db =>
        {
            db.PropFirms.Add(firm);
            var active = Account(firm, "u1");
            var activeSameUser = Account(firm, "u1", AccountStage.Evaluation);
            var closed = Account(firm, "u2", AccountStage.Failed);
            db.TradingAccounts.AddRange(active, activeSameUser, closed);
            db.Users.AddRange(
                new Infrastructure.Identity.ApplicationUser { Id = "u1", Email = "u1@test.dev" },
                new Infrastructure.Identity.ApplicationUser { Id = "u2", Email = "u2@test.dev" });
        });
        var sender = new RecordingEmailSender();
        var service = BuildService(factory, sender);

        await service.UpdateAsync(firm.Id, new UpsertPropFirmRequest(
            "Lucid", null, null, null, FirmHealthStatus.Watch, null, "Retrasos de payout reportados"));

        // u1 (2 cuentas activas) recibe un solo email; u2 (cuenta cerrada) no recibe nada.
        var email = Assert.Single(sender.Sent);
        Assert.Equal("u1@test.dev", email.To);

        var updated = await service.GetByIdAsync(firm.Id);
        Assert.Equal(FirmHealthStatus.Watch, updated!.HealthStatus);
        Assert.NotNull(updated.HealthUpdatedOn);
    }

    [Fact]
    public async Task Update_WithoutHealthStatusChange_SendsNoEmail()
    {
        var firm = new PropFirm { Id = Guid.NewGuid(), Name = "Apex", HealthStatus = FirmHealthStatus.Active };
        var factory = await SeedAsync(db =>
        {
            db.PropFirms.Add(firm);
            var account = Account(firm, "u1");
            db.TradingAccounts.Add(account);
            db.Users.Add(new Infrastructure.Identity.ApplicationUser { Id = "u1", Email = "u1@test.dev" });
        });
        var sender = new RecordingEmailSender();
        var service = BuildService(factory, sender);

        await service.UpdateAsync(firm.Id, new UpsertPropFirmRequest(
            "Apex renombrada", null, null, 8, FirmHealthStatus.Active));

        Assert.Empty(sender.Sent);
    }
}
