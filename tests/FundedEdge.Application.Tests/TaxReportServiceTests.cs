using FundedEdge.Application.Dtos;
using FundedEdge.Domain.Entities;
using FundedEdge.Domain.Enums;
using FundedEdge.Infrastructure.Persistence;
using FundedEdge.Infrastructure.Services;

namespace FundedEdge.Application.Tests;

public class TaxReportServiceTests
{
    private const string UserId = "user-1";
    private const string OtherUserId = "user-2";

    private static async Task<InMemoryDbContextFactory> SeedAsync(Action<FundedEdgeDbContext> seed)
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        await using var db = await factory.CreateDbContextAsync();
        seed(db);
        await db.SaveChangesAsync();
        return factory;
    }

    private static TaxReportService BuildService(InMemoryDbContextFactory factory) =>
        new(factory, new FakeCurrentUserAccessor(UserId));

    private static TradingAccount Account(string userId, PropFirm firm, string name) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        PropFirm = firm,
        PropFirmId = firm.Id,
        DisplayName = name,
        AccountSize = 50_000m,
        ProfitTarget = 3_000m,
        MaxDrawdown = 2_500m,
        PurchasedOn = new DateOnly(2025, 1, 1),
    };

    [Fact]
    public async Task GetYearReport_CrossYearPayout_CountsByPaidOnYear()
    {
        var firm = new PropFirm { Id = Guid.NewGuid(), Name = "Apex" };
        TradingAccount account = null!;
        var factory = await SeedAsync(db =>
        {
            account = Account(UserId, firm, "Apex 50K");
            db.TradingAccounts.Add(account);
            // Solicitado en diciembre 2025, cobrado en enero 2026 → cuenta en 2026 (criterio de caja).
            db.Payouts.Add(new Payout
            {
                AccountId = account.Id,
                AmountRequested = 1_000m,
                AmountReceived = 900m,
                RequestedOn = new DateOnly(2025, 12, 28),
                PaidOn = new DateOnly(2026, 1, 5),
                Status = PayoutStatus.Paid,
            });
        });
        var service = BuildService(factory);

        var report2025 = await service.GetYearReportAsync(2025);
        var report2026 = await service.GetYearReportAsync(2026);

        Assert.Empty(report2025.Payouts);
        Assert.Single(report2026.Payouts);
        Assert.Equal(900m, report2026.TotalPayoutsReceived);
        Assert.Equal(900m, report2026.Quarters.Single(q => q.Quarter == 1).PayoutsReceived);
    }

    [Fact]
    public async Task GetYearReport_IgnoresUnpaidPayoutsAndOtherUsers()
    {
        var firm = new PropFirm { Id = Guid.NewGuid(), Name = "Tradeify" };
        var factory = await SeedAsync(db =>
        {
            var mine = Account(UserId, firm, "Mine");
            var theirs = Account(OtherUserId, firm, "Theirs");
            db.TradingAccounts.AddRange(mine, theirs);
            db.Payouts.Add(new Payout
            {
                AccountId = mine.Id, AmountRequested = 500m, AmountReceived = 0m,
                RequestedOn = new DateOnly(2026, 2, 1), Status = PayoutStatus.Requested,
            });
            db.Payouts.Add(new Payout
            {
                AccountId = theirs.Id, AmountRequested = 700m, AmountReceived = 700m,
                RequestedOn = new DateOnly(2026, 2, 1), PaidOn = new DateOnly(2026, 2, 10),
                Status = PayoutStatus.Paid,
            });
            db.AccountCosts.Add(new AccountCost
            {
                AccountId = theirs.Id, Kind = CostKind.Evaluation, Amount = 150m,
                PaidOn = new DateOnly(2026, 2, 1),
            });
        });
        var service = BuildService(factory);

        var report = await service.GetYearReportAsync(2026);

        Assert.Empty(report.Payouts);
        Assert.Empty(report.Costs);
        Assert.Equal(0m, report.Net);
    }

    [Fact]
    public async Task GetYearReport_AggregatesCostsByQuarterAndComputesNet()
    {
        var firm = new PropFirm { Id = Guid.NewGuid(), Name = "Lucid" };
        var factory = await SeedAsync(db =>
        {
            var account = Account(UserId, firm, "Lucid 100K");
            db.TradingAccounts.Add(account);
            db.AccountCosts.AddRange(
                new AccountCost { AccountId = account.Id, Kind = CostKind.Evaluation, Amount = 100m, PaidOn = new DateOnly(2026, 1, 10) },
                new AccountCost { AccountId = account.Id, Kind = CostKind.Reset, Amount = 80m, PaidOn = new DateOnly(2026, 3, 31) },
                new AccountCost { AccountId = account.Id, Kind = CostKind.MonthlyFee, Amount = 50m, PaidOn = new DateOnly(2026, 7, 1) });
            db.Payouts.Add(new Payout
            {
                AccountId = account.Id, AmountRequested = 1_200m, AmountReceived = 1_200m,
                RequestedOn = new DateOnly(2026, 6, 20), PaidOn = new DateOnly(2026, 7, 2),
                Status = PayoutStatus.Paid,
            });
        });
        var service = BuildService(factory);

        var report = await service.GetYearReportAsync(2026);

        Assert.Equal(230m, report.TotalCosts);
        Assert.Equal(1_200m, report.TotalPayoutsReceived);
        Assert.Equal(970m, report.Net);
        Assert.Equal(180m, report.Quarters.Single(q => q.Quarter == 1).Costs);
        var q3 = report.Quarters.Single(q => q.Quarter == 3);
        Assert.Equal(50m, q3.Costs);
        Assert.Equal(1_200m, q3.PayoutsReceived);
        Assert.Equal(1_150m, q3.Net);
    }

    [Fact]
    public async Task GetAvailableYears_UnionOfPayoutAndCostYears_Descending()
    {
        var firm = new PropFirm { Id = Guid.NewGuid(), Name = "Apex" };
        var factory = await SeedAsync(db =>
        {
            var account = Account(UserId, firm, "Apex 50K");
            db.TradingAccounts.Add(account);
            db.AccountCosts.Add(new AccountCost { AccountId = account.Id, Kind = CostKind.Evaluation, Amount = 100m, PaidOn = new DateOnly(2024, 5, 1) });
            db.Payouts.Add(new Payout
            {
                AccountId = account.Id, AmountRequested = 300m, AmountReceived = 300m,
                RequestedOn = new DateOnly(2026, 1, 1), PaidOn = new DateOnly(2026, 1, 15),
                Status = PayoutStatus.Paid,
            });
        });
        var service = BuildService(factory);

        Assert.Equal([2026, 2024], await service.GetAvailableYearsAsync());
    }

    [Fact]
    public void Csv_EscapesSeparatorsAndUsesInvariantFormat()
    {
        var report = new TaxYearReportDto(
            2026,
            [new TaxPayoutLineDto(new DateOnly(2026, 3, 5), "Apex, Inc", "Cuenta \"A\"", 1234.5m, null)],
            [new TaxCostLineDto(new DateOnly(2026, 1, 2), "Apex, Inc", "Cuenta \"A\"", CostKind.Reset, 80m, "nota, con coma")],
            []);

        var csv = TaxReportCsv.Build(report);
        var lines = csv.TrimEnd().Split('\n').Select(l => l.TrimEnd('\r')).ToArray();

        Assert.Equal("Type,Date,Firm,Account,Concept,Amount,Notes", lines[0]);
        Assert.Equal("Payout,2026-03-05,\"Apex, Inc\",\"Cuenta \"\"A\"\"\",Payout,1234.5,", lines[1]);
        Assert.Equal("Cost,2026-01-02,\"Apex, Inc\",\"Cuenta \"\"A\"\"\",Reset,80,\"nota, con coma\"", lines[2]);
    }
}
