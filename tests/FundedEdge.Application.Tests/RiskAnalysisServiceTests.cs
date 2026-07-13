using FundedEdge.Application.Dtos;
using FundedEdge.Domain.Entities;
using FundedEdge.Domain.Enums;
using FundedEdge.Infrastructure.Services;

namespace FundedEdge.Application.Tests;

public class RiskAnalysisServiceTests
{
    private const string UserId = "user-1";

    private static RiskAnalysisService BuildService(InMemoryDbContextFactory factory) =>
        new(factory, new FakeCurrentUserAccessor(UserId));

    /// <summary>
    /// Siembra un funnel conocido: 2 fondeadas (payouts 2000 y 0) y 2 falladas, todas con coste
    /// de evaluación 150; la primera fondeada además con activación 100. Pass rate esperado: 0.5.
    /// </summary>
    private static async Task<(InMemoryDbContextFactory Factory, Guid FundedAccountId)> SeedFunnelAsync()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var firmId = Guid.NewGuid();
        var fundedId = Guid.NewGuid();

        await using var db = await factory.CreateDbContextAsync();
        db.PropFirms.Add(new PropFirm { Id = firmId, Name = "Firm" });

        void AddAccount(Guid id, AccountStage stage, DateOnly? fundedOn, decimal payout, decimal activation)
        {
            var account = new TradingAccount
            {
                Id = id,
                UserId = UserId,
                PropFirmId = firmId,
                DisplayName = $"Acc-{id.ToString()[..4]}",
                AccountSize = 50_000,
                ProfitTarget = 3_000m,
                MaxDrawdown = 2_000m,
                DrawdownType = DrawdownType.Trailing,
                Stage = stage,
                FundedOn = fundedOn,
                PurchasedOn = new DateOnly(2026, 1, 1),
            };
            account.Costs.Add(new AccountCost { AccountId = id, Kind = CostKind.Evaluation, Amount = 150m, PaidOn = new DateOnly(2026, 1, 1) });
            if (activation > 0)
            {
                account.Costs.Add(new AccountCost { AccountId = id, Kind = CostKind.Activation, Amount = activation, PaidOn = new DateOnly(2026, 2, 1) });
            }
            if (payout > 0)
            {
                account.Payouts.Add(new Payout
                {
                    AccountId = id,
                    AmountRequested = payout,
                    AmountReceived = payout,
                    RequestedOn = new DateOnly(2026, 3, 1),
                    Status = PayoutStatus.Paid,
                });
            }
            db.TradingAccounts.Add(account);
        }

        AddAccount(fundedId, AccountStage.Funded, new DateOnly(2026, 2, 1), payout: 2_000m, activation: 100m);
        AddAccount(Guid.NewGuid(), AccountStage.Failed, fundedOn: new DateOnly(2026, 2, 1), payout: 0m, activation: 0m); // fondeada y quemada sin payout
        AddAccount(Guid.NewGuid(), AccountStage.Failed, fundedOn: null, payout: 0m, activation: 0m);
        AddAccount(Guid.NewGuid(), AccountStage.Expired, fundedOn: null, payout: 0m, activation: 0m);

        await db.SaveChangesAsync();
        return (factory, fundedId);
    }

    [Fact]
    public async Task GetDefaultsAsync_ComputesObservedFunnelValues()
    {
        var (factory, _) = await SeedFunnelAsync();
        var sut = BuildService(factory);

        var defaults = await sut.GetDefaultsAsync();

        Assert.Equal(4, defaults.EvaluationsTerminated);
        Assert.Equal(2, defaults.FundedAccounts);
        Assert.Equal(0.5, defaults.PassRate);
        Assert.Equal(150m, defaults.AvgEvaluationCost);
        Assert.Equal(100m, defaults.AvgActivationCost);
        Assert.Equal([2_000m, 0m], defaults.PayoutsPerFundedAccount.OrderByDescending(p => p));

        // EV observado = media de resultados netos: (2000-250) + (-150) + (-150) + (-150) = 1300 / 4 = 325
        Assert.NotNull(defaults.Ev);
        Assert.Equal(325m, defaults.Ev!.EvPerEvaluation);
        Assert.Equal(4, defaults.Ev.SampleSize);
    }

    [Fact]
    public async Task GetDefaultsAsync_EmptyDatabase_ReturnsNullsWithoutThrowing()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var sut = BuildService(factory);

        var defaults = await sut.GetDefaultsAsync();

        Assert.Null(defaults.PassRate);
        Assert.Null(defaults.Ev);
        Assert.Null(defaults.KellyFraction);
        Assert.Empty(defaults.PayoutsPerFundedAccount);
    }

    [Fact]
    public async Task RunBankrollPlanAsync_UsesObservedDefaults_AndReportsInputsUsed()
    {
        var (factory, _) = await SeedFunnelAsync();
        var sut = BuildService(factory);

        var result = await sut.RunBankrollPlanAsync(new BankrollPlanRequest(
            Bankroll: 3_000m, MonthlyEvaluationBudget: 2, Months: 12, Iterations: 2_000));

        Assert.Equal(0.5, result.InputsUsed.PassRate);
        Assert.Equal(150m, result.InputsUsed.EvaluationCost);
        Assert.Equal(100m, result.InputsUsed.ActivationCost);
        Assert.InRange(result.Simulation.ProbabilityOfRuin, 0.0, 1.0);
        Assert.NotNull(result.MinimumBankrollFor5PctRuin);
    }

    [Fact]
    public async Task RunBankrollPlanAsync_OverridesTakePriorityOverObserved()
    {
        var (factory, _) = await SeedFunnelAsync();
        var sut = BuildService(factory);

        var result = await sut.RunBankrollPlanAsync(new BankrollPlanRequest(
            Bankroll: 3_000m, MonthlyEvaluationBudget: 2, Months: 6,
            EvaluationCostOverride: 200m, PassRateOverride: 0.25, Iterations: 1_000));

        Assert.Equal(0.25, result.InputsUsed.PassRate);
        Assert.Equal(200m, result.InputsUsed.EvaluationCost);
    }

    [Fact]
    public async Task RunBankrollPlanAsync_NoDataAndNoOverrides_ThrowsWithClearMessage()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var sut = BuildService(factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.RunBankrollPlanAsync(new BankrollPlanRequest(1_000m, 2, 12)));

        Assert.Contains("pass rate", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAccountSimulationAsync_FewOwnTrades_FallsBackToGlobalDistribution()
    {
        var (factory, fundedId) = await SeedFunnelAsync();

        // 5 trades globales (en la cuenta fondeada): por debajo del mínimo de 20 propios,
        // así que la simulación usa la distribución global (que aquí coincide con la propia).
        await using (var db = await factory.CreateDbContextAsync())
        {
            for (int i = 0; i < 5; i++)
            {
                db.Trades.Add(new Trade
                {
                    AccountId = fundedId,
                    Symbol = "MES",
                    Direction = TradeDirection.Long,
                    Quantity = 1,
                    OpenedAt = DateTimeOffset.UtcNow,
                    ClosedAt = DateTimeOffset.UtcNow,
                    GrossPnL = i % 2 == 0 ? 150m : -100m,
                    Commissions = 2m,
                });
            }
            await db.SaveChangesAsync();
        }

        var sut = BuildService(factory);
        var result = await sut.RunAccountSimulationAsync(fundedId);

        Assert.NotNull(result);
        Assert.True(result!.UsedGlobalTrades);
        Assert.Equal(5, result.TradesSampled);
        var probs = result.Simulation.ProbabilityOfReachingTarget + result.Simulation.ProbabilityOfBusting + result.Simulation.ProbabilityOfTimeout;
        Assert.Equal(1.0, probs, precision: 9);
    }

    [Fact]
    public async Task RunAccountSimulationAsync_NoTradesAnywhere_ReturnsNull()
    {
        var (factory, fundedId) = await SeedFunnelAsync();
        var sut = BuildService(factory);

        Assert.Null(await sut.RunAccountSimulationAsync(fundedId));
    }

    [Fact]
    public async Task RunAccountSimulationAsync_UnknownAccount_ReturnsNull()
    {
        var (factory, _) = await SeedFunnelAsync();
        var sut = BuildService(factory);

        Assert.Null(await sut.RunAccountSimulationAsync(Guid.NewGuid()));
    }

    private static async Task<InMemoryDbContextFactory> SeedAccountWithTradesAsync(
        AccountStage stage, DrawdownType drawdownType, decimal maxDrawdown, params decimal[] tradePnLs)
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var firmId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        await using var db = await factory.CreateDbContextAsync();
        db.PropFirms.Add(new PropFirm { Id = firmId, Name = "Firm" });
        db.TradingAccounts.Add(new TradingAccount
        {
            Id = accountId,
            UserId = UserId,
            PropFirmId = firmId,
            DisplayName = "Cuenta en riesgo",
            AccountSize = 50_000,
            ProfitTarget = 3_000m,
            MaxDrawdown = maxDrawdown,
            DrawdownType = drawdownType,
            Stage = stage,
            PurchasedOn = new DateOnly(2026, 1, 1),
        });

        var closedAt = DateTimeOffset.UtcNow.AddDays(-tradePnLs.Length);
        foreach (var pnl in tradePnLs)
        {
            db.Trades.Add(new Trade
            {
                AccountId = accountId,
                Symbol = "MES",
                Direction = TradeDirection.Long,
                Quantity = 1,
                OpenedAt = closedAt,
                ClosedAt = closedAt,
                GrossPnL = pnl,
                Commissions = 0m,
            });
            closedAt = closedAt.AddDays(1);
        }

        await db.SaveChangesAsync();
        return factory;
    }

    [Fact]
    public async Task GetDrawdownAlertsAsync_TrailingDrawdown_ConsumedOver80Pct_TriggersAlert()
    {
        // Pico de equity +1000, ahora en +100: colchón consumido = (1000-100)/2000 = 45%... por eso subimos MaxDrawdown pequeño.
        // Con MaxDrawdown 1000: suelo = 1000-1000=0; equity actual 100; buffer restante 100/1000 = 10% -> consumido 90%.
        var factory = await SeedAccountWithTradesAsync(AccountStage.Funded, DrawdownType.Trailing, maxDrawdown: 1_000m, 1_000m, -900m);
        var sut = BuildService(factory);

        var alerts = await sut.GetDrawdownAlertsAsync();

        var alert = Assert.Single(alerts);
        Assert.Equal(0.9, alert.ConsumedFraction, precision: 6);
        Assert.Equal(100m, alert.RemainingBuffer);
    }

    [Fact]
    public async Task GetDrawdownAlertsAsync_WellBelowThreshold_NoAlert()
    {
        var factory = await SeedAccountWithTradesAsync(AccountStage.Funded, DrawdownType.Trailing, maxDrawdown: 2_000m, 500m);
        var sut = BuildService(factory);

        Assert.Empty(await sut.GetDrawdownAlertsAsync());
    }

    [Fact]
    public async Task GetDrawdownAlertsAsync_StaticDrawdown_AnchorsFloorToInitialBalance()
    {
        // Static: el suelo no sube con el pico ganador previo, se queda anclado al balance inicial (0).
        // Con MaxDrawdown 1000, suelo = -1000; tras +1000 y luego -1900 la equity queda en -900;
        // buffer restante = -900 - (-1000) = 100 -> consumido 90 %. Con Trailing el suelo habría
        // subido a 1000-1000=0 y ya estaría quemada; Static es más permisivo por diseño.
        var factory = await SeedAccountWithTradesAsync(AccountStage.Funded, DrawdownType.Static, maxDrawdown: 1_000m, 1_000m, -1_900m);
        var sut = BuildService(factory);

        var alert = Assert.Single(await sut.GetDrawdownAlertsAsync());
        Assert.Equal(0.9, alert.ConsumedFraction, precision: 6);
    }

    [Fact]
    public async Task GetDrawdownAlertsAsync_TerminalAccount_IsIgnored()
    {
        var factory = await SeedAccountWithTradesAsync(AccountStage.Failed, DrawdownType.Trailing, maxDrawdown: 1_000m, 1_000m, -950m);
        var sut = BuildService(factory);

        Assert.Empty(await sut.GetDrawdownAlertsAsync());
    }
}
