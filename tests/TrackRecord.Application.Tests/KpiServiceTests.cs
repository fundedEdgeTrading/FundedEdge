using Microsoft.EntityFrameworkCore;
using TrackRecord.Domain.Entities;
using TrackRecord.Domain.Enums;
using TrackRecord.Infrastructure.Persistence;
using TrackRecord.Infrastructure.Services;

namespace TrackRecord.Application.Tests;

/// <summary>
/// Fábrica de DbContext sobre el proveedor InMemory de EF Core, para testear la lógica
/// de agregación del KpiService sin depender de una instancia real de SQL Server.
/// </summary>
public sealed class InMemoryDbContextFactory(string databaseName) : IDbContextFactory<TrackRecordDbContext>
{
    public TrackRecordDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TrackRecordDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new TrackRecordDbContext(options);
    }

    public Task<TrackRecordDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(CreateDbContext());
}

public class KpiServiceTests
{
    private const string UserId = "user-1";

    private static async Task<InMemoryDbContextFactory> SeedAsync(Action<TrackRecordDbContext> seed)
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        await using var db = await factory.CreateDbContextAsync();
        seed(db);
        await db.SaveChangesAsync();
        return factory;
    }

    private static KpiService BuildService(InMemoryDbContextFactory factory) =>
        new(factory, new FakeCurrentUserAccessor(UserId));

    [Fact]
    public async Task GetBusinessKpisAsync_ComputesPassRateAndRoi()
    {
        var propFirmId = Guid.NewGuid();

        var factory = await SeedAsync(db =>
        {
            db.PropFirms.Add(new PropFirm { Id = propFirmId, Name = "Test Firm" });

            // 1 fondeada con payout, 1 fallida en evaluación, 1 todavía en evaluación (no cuenta como terminada)
            db.TradingAccounts.AddRange(
                new TradingAccount
                {
                    UserId = UserId,
                    PropFirmId = propFirmId,
                    DisplayName = "Funded #1",
                    AccountSize = 50_000,
                    Stage = AccountStage.Funded,
                    FundedOn = new DateOnly(2026, 2, 1),
                    PurchasedOn = new DateOnly(2026, 1, 1),
                    Costs = { new AccountCost { Kind = CostKind.Evaluation, Amount = 150m, PaidOn = new DateOnly(2026, 1, 1) } },
                    Payouts = { new Payout { AmountRequested = 1000m, AmountReceived = 900m, RequestedOn = new DateOnly(2026, 3, 1), Status = PayoutStatus.Paid } },
                },
                new TradingAccount
                {
                    UserId = UserId,
                    PropFirmId = propFirmId,
                    DisplayName = "Failed #1",
                    AccountSize = 50_000,
                    Stage = AccountStage.Failed,
                    PurchasedOn = new DateOnly(2026, 1, 1),
                    ClosedOn = new DateOnly(2026, 1, 15),
                    Costs = { new AccountCost { Kind = CostKind.Evaluation, Amount = 150m, PaidOn = new DateOnly(2026, 1, 1) } },
                },
                new TradingAccount
                {
                    UserId = UserId,
                    PropFirmId = propFirmId,
                    DisplayName = "In Progress #1",
                    AccountSize = 50_000,
                    Stage = AccountStage.Evaluation,
                    PurchasedOn = new DateOnly(2026, 4, 1),
                    Costs = { new AccountCost { Kind = CostKind.Evaluation, Amount = 150m, PaidOn = new DateOnly(2026, 4, 1) } },
                });
        });

        var sut = BuildService(factory);
        var result = await sut.GetBusinessKpisAsync();

        Assert.Equal(3, result.AccountsPurchased);
        Assert.Equal(1, result.AccountsFunded);
        Assert.Equal(1, result.AccountsFailed);
        Assert.Equal(1, result.AccountsInEvaluation);
        Assert.Equal(2, result.EvaluationsTerminated);      // Funded + Failed; la que sigue en evaluación no cuenta
        Assert.Equal(0.5, result.PassRate);                 // 1 de 2 terminadas pasó
        Assert.Equal(450m, result.TotalCosts);               // 150 * 3
        Assert.Equal(900m, result.TotalPayoutsReceived);
        Assert.Equal(450m, result.NetCashflow);
        Assert.Equal(450m, result.CostPerFundedAccount);     // coste TOTAL del funnel (450) / cuentas fondeadas (1)
        Assert.Equal(900m, result.AvgPayoutPerFundedAccount);
        Assert.Equal(1.0, result.BusinessRoi!.Value, precision: 5); // (900-450)/450 = 1.0
    }

    [Fact]
    public async Task GetTradingKpisAsync_ComputesWinRateProfitFactorAndDrawdown()
    {
        var accountId = Guid.NewGuid();

        var factory = await SeedAsync(db =>
        {
            db.TradingAccounts.Add(new TradingAccount
            {
                Id = accountId,
                UserId = UserId,
                DisplayName = "Acc",
                AccountSize = 50_000,
                PurchasedOn = new DateOnly(2026, 1, 1),
            });

            DateTimeOffset D(int day) => new(2026, 1, day, 0, 0, 0, TimeSpan.Zero);

            db.Trades.AddRange(
                new Trade { AccountId = accountId, Symbol = "MES", OpenedAt = D(1), ClosedAt = D(1), GrossPnL = 200m, Commissions = 0m, RiskedAmount = 100m },
                new Trade { AccountId = accountId, Symbol = "MES", OpenedAt = D(2), ClosedAt = D(2), GrossPnL = -100m, Commissions = 0m, RiskedAmount = 100m },
                new Trade { AccountId = accountId, Symbol = "MES", OpenedAt = D(3), ClosedAt = D(3), GrossPnL = -100m, Commissions = 0m, RiskedAmount = 100m },
                new Trade { AccountId = accountId, Symbol = "MES", OpenedAt = D(4), ClosedAt = D(4), GrossPnL = 300m, Commissions = 0m, RiskedAmount = 100m });
        });

        var sut = BuildService(factory);
        var result = await sut.GetTradingKpisAsync();

        Assert.Equal(4, result.TotalTrades);
        Assert.Equal(2, result.WinningTrades);
        Assert.Equal(2, result.LosingTrades);
        Assert.Equal(0.5, result.WinRate);
        Assert.Equal(300m, result.NetPnL);                 // 200 - 100 - 100 + 300
        Assert.Equal(500m, result.GrossProfit);
        Assert.Equal(200m, result.GrossLoss);
        Assert.Equal(2.5, result.ProfitFactor!.Value, precision: 5);
        // Equity: 200 -> 100 -> 0 -> 300. Peak 200 en día1, mínimo 0 en día3 => drawdown máx 200.
        Assert.Equal(200m, result.MaxDrawdown);
        Assert.Equal(2, result.MaxConsecutiveLosses);
        Assert.Equal(1, result.MaxConsecutiveWins);
        Assert.Equal(0.75, result.AvgRMultiple!.Value, precision: 5); // avg(2, -1, -1, 3) = 0.75
    }

    [Fact]
    public async Task GetTagPerformanceAsync_GroupsByTagAndComputesPerTagMetrics()
    {
        var accountId = Guid.NewGuid();

        var factory = await SeedAsync(db =>
        {
            db.TradingAccounts.Add(new TradingAccount
            {
                Id = accountId,
                UserId = UserId,
                DisplayName = "Acc",
                AccountSize = 50_000,
                PurchasedOn = new DateOnly(2026, 1, 1),
            });

            db.Trades.AddRange(
                // "breakout" gana 200, pierde 100 => WinRate 0.5, PF 2.0, NetPnL 100
                new Trade { AccountId = accountId, Symbol = "MES", GrossPnL = 200m, Commissions = 0m, Tags = "breakout" },
                new Trade { AccountId = accountId, Symbol = "MES", GrossPnL = -100m, Commissions = 0m, Tags = "breakout, news" },
                // "news" solo el trade de arriba: pierde 100 => WinRate 0, sin ganancias => PF null
                // "scalp" un único trade ganador
                new Trade { AccountId = accountId, Symbol = "MES", GrossPnL = 50m, Commissions = 0m, Tags = "scalp" },
                // Sin tags: no debe aparecer en el desglose
                new Trade { AccountId = accountId, Symbol = "MES", GrossPnL = 1000m, Commissions = 0m, Tags = null });
        });

        var sut = BuildService(factory);
        var result = await sut.GetTagPerformanceAsync();

        Assert.Equal(3, result.Count); // breakout, news, scalp — no incluye el trade sin tags

        var breakout = result.Single(t => t.Tag == "breakout");
        Assert.Equal(2, breakout.TotalTrades);
        Assert.Equal(0.5, breakout.WinRate);
        Assert.Equal(2.0, breakout.ProfitFactor!.Value, precision: 5);
        Assert.Equal(100m, breakout.NetPnL);

        var news = result.Single(t => t.Tag == "news");
        Assert.Equal(1, news.TotalTrades);
        Assert.Equal(0.0, news.WinRate);
        Assert.Equal(0.0, news.ProfitFactor!.Value, precision: 5); // sin ganancias: 0 / pérdida = 0
        Assert.Equal(-100m, news.NetPnL);

        var scalp = result.Single(t => t.Tag == "scalp");
        Assert.Equal(1, scalp.TotalTrades);
        Assert.Null(scalp.ProfitFactor); // sin pérdidas que dividir (evita división por cero)
        Assert.Equal(50m, scalp.NetPnL);

        // Orden descendente por Net P&L: breakout (100) > scalp (50) > news (-100).
        Assert.Equal(["breakout", "scalp", "news"], result.Select(t => t.Tag));
    }

    [Fact]
    public async Task GetTagPerformanceAsync_NoTaggedTrades_ReturnsEmpty()
    {
        var accountId = Guid.NewGuid();
        var factory = await SeedAsync(db =>
        {
            db.TradingAccounts.Add(new TradingAccount
            {
                Id = accountId,
                UserId = UserId,
                DisplayName = "Acc",
                AccountSize = 50_000,
                PurchasedOn = new DateOnly(2026, 1, 1),
            });
            db.Trades.Add(new Trade { AccountId = accountId, Symbol = "MES", GrossPnL = 100m, Commissions = 0m });
        });

        var sut = BuildService(factory);
        Assert.Empty(await sut.GetTagPerformanceAsync());
    }
}
