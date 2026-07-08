using TrackRecord.Domain.Entities;
using TrackRecord.Domain.Enums;
using TrackRecord.Infrastructure.Identity;
using TrackRecord.Infrastructure.Services;

namespace TrackRecord.Application.Tests;

public class FirmFitServiceTests
{
    private const string UserId = "user-1";
    private static readonly Guid FirmId = Guid.NewGuid();

    private static FirmFitService BuildService(InMemoryDbContextFactory factory)
    {
        var accessor = new FakeCurrentUserAccessor(UserId);
        return new FirmFitService(factory, accessor, new PlanService(factory, accessor));
    }

    private static async Task<InMemoryDbContextFactory> SeedAsync(
        PlanTier tier,
        IReadOnlyList<decimal>? tradePnls,
        decimal? fundedPayout,
        params EvaluationProgram[] programs)
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        await using var db = await factory.CreateDbContextAsync();

        db.Users.Add(new ApplicationUser { Id = UserId, UserName = "user@test.com", PlanTier = tier });
        db.PropFirms.Add(new PropFirm { Id = FirmId, Name = "Test Firm" });

        if (tradePnls is { Count: > 0 })
        {
            var evalAccountId = Guid.NewGuid();
            db.TradingAccounts.Add(new TradingAccount
            {
                Id = evalAccountId,
                UserId = UserId,
                PropFirmId = FirmId,
                DisplayName = "Eval",
                AccountSize = 50_000m,
                PurchasedOn = new DateOnly(2026, 1, 1),
                Stage = AccountStage.Evaluation,
            });

            // Un trade por día en días distintos ⇒ TradesPerDay = 1 (cadencia determinista).
            for (var i = 0; i < tradePnls.Count; i++)
            {
                db.Trades.Add(new Trade
                {
                    Id = Guid.NewGuid(),
                    AccountId = evalAccountId,
                    Symbol = "MES",
                    Direction = TradeDirection.Long,
                    Quantity = 1,
                    ClosedAt = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero).AddDays(i),
                    OpenedAt = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero).AddDays(i),
                    GrossPnL = tradePnls[i],
                    Commissions = 0m,
                });
            }
        }

        if (fundedPayout is { } payout)
        {
            var fundedId = Guid.NewGuid();
            var funded = new TradingAccount
            {
                Id = fundedId,
                UserId = UserId,
                PropFirmId = FirmId,
                DisplayName = "Funded",
                AccountSize = 50_000m,
                PurchasedOn = new DateOnly(2026, 1, 1),
                FundedOn = new DateOnly(2026, 2, 1),
                Stage = AccountStage.Funded,
            };
            funded.Payouts.Add(new Payout
            {
                AccountId = fundedId,
                AmountRequested = payout,
                AmountReceived = payout,
                RequestedOn = new DateOnly(2026, 3, 1),
                Status = PayoutStatus.Paid,
            });
            db.TradingAccounts.Add(funded);
        }

        db.EvaluationPrograms.AddRange(programs);
        await db.SaveChangesAsync();
        return factory;
    }

    private static EvaluationProgram Program(string name, decimal target, decimal cost, decimal? dailyLoss = null, int? minDays = null, decimal? consistency = null) => new()
    {
        Id = Guid.NewGuid(),
        PropFirmId = FirmId,
        Name = name,
        AccountSize = 50_000m,
        EvaluationCost = cost,
        ActivationCost = 0m,
        ProfitTarget = target,
        MaxDrawdown = 2_000m,
        DrawdownType = DrawdownType.Trailing,
        DailyLossLimit = dailyLoss,
        MinTradingDays = minDays,
        ConsistencyMaxDayFraction = consistency,
        EffectiveFrom = new DateOnly(2026, 1, 1),
        IsActive = true,
    };

    private static IReadOnlyList<decimal> WinningStreak(int count) =>
        Enumerable.Repeat(100m, count).ToList();

    [Fact]
    public async Task RankProgramsAsync_NoTrades_ReturnsEmptyRanking()
    {
        var factory = await SeedAsync(PlanTier.Pro, tradePnls: null, fundedPayout: null, Program("A", 2_000m, 150m));

        var result = await BuildService(factory).RankProgramsAsync();

        Assert.Empty(result.Programs);
        Assert.Equal(0, result.TradesAnalyzed);
    }

    [Fact]
    public async Task RankProgramsAsync_ProPlan_RanksAllProgramsByEvDescending()
    {
        // Ambos programas se pasan con la misma probabilidad (racha ganadora), así que el más barato
        // tiene mayor EV y debe quedar primero.
        var factory = await SeedAsync(
            PlanTier.Pro,
            WinningStreak(30),
            fundedPayout: 2_000m,
            Program("Caro", 1_000m, 300m),
            Program("Barato", 1_000m, 150m));

        var result = await BuildService(factory).RankProgramsAsync();

        Assert.Equal(2, result.Programs.Count);
        Assert.False(result.IsLimitedByPlan);
        Assert.Equal(2_000m, result.AvgPayoutPerFundedAccount);
        Assert.All(result.Programs, p => Assert.NotNull(p.EvPerEvaluation));
        Assert.All(result.Programs, p => Assert.InRange(p.FitScore, 0, 100));
        Assert.True(result.Programs[0].EvPerEvaluation >= result.Programs[1].EvPerEvaluation);
        Assert.Equal("Barato", result.Programs[0].ProgramName);
    }

    [Fact]
    public async Task RankProgramsAsync_StarterPlan_LimitsToTopProgram()
    {
        var factory = await SeedAsync(
            PlanTier.Starter,
            WinningStreak(30),
            fundedPayout: 2_000m,
            Program("A", 1_000m, 150m),
            Program("B", 1_000m, 300m));

        var result = await BuildService(factory).RankProgramsAsync();

        Assert.True(result.IsLimitedByPlan);
        Assert.Single(result.Programs);
    }

    [Fact]
    public async Task RankProgramsAsync_FewTrades_FlagsLowConfidence()
    {
        var factory = await SeedAsync(PlanTier.Pro, WinningStreak(5), fundedPayout: 2_000m, Program("A", 1_000m, 150m));

        var result = await BuildService(factory).RankProgramsAsync();

        Assert.True(result.LowConfidence);
        Assert.Equal(5, result.TradesAnalyzed);
    }

    [Fact]
    public async Task RankProgramsAsync_NoPayouts_LeavesEvNull()
    {
        var factory = await SeedAsync(PlanTier.Pro, WinningStreak(30), fundedPayout: null, Program("A", 1_000m, 150m));

        var result = await BuildService(factory).RankProgramsAsync();

        Assert.Single(result.Programs);
        Assert.Null(result.AvgPayoutPerFundedAccount);
        Assert.Null(result.Programs[0].EvPerEvaluation);
    }

    [Fact]
    public async Task RankProgramsAsync_ProgramWithRules_ReportsPerRuleImpacts()
    {
        var factory = await SeedAsync(
            PlanTier.Pro,
            WinningStreak(30),
            fundedPayout: 2_000m,
            Program("Con reglas", 1_000m, 150m, dailyLoss: 800m, minDays: 5, consistency: 0.30m));

        var result = await BuildService(factory).RankProgramsAsync();

        var program = Assert.Single(result.Programs);
        var keys = program.RuleImpacts.Select(r => r.RuleKey).ToList();
        Assert.Contains("daily-loss", keys);
        Assert.Contains("consistency", keys);
        Assert.Contains("min-trading-days", keys);
    }
}
