using TrackRecord.Domain.Entities;
using TrackRecord.Domain.Enums;

namespace TrackRecord.Domain.Tests;

public class TradingAccountTests
{
    private static TradingAccount CreateAccount() => new()
    {
        DisplayName = "Apex 50K #1",
        AccountSize = 50_000m,
        Stage = AccountStage.Evaluation,
        PurchasedOn = new DateOnly(2026, 1, 1),
    };

    [Fact]
    public void TransitionTo_Funded_SetsFundedOnAndAppendsEvent()
    {
        var account = CreateAccount();
        var occurredAt = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

        var evt = account.TransitionTo(AccountStage.Funded, occurredAt, "Evaluación superada");

        Assert.Equal(AccountStage.Funded, account.Stage);
        Assert.Equal(new DateOnly(2026, 2, 1), account.FundedOn);
        Assert.Null(account.ClosedOn);
        Assert.Single(account.Events);
        Assert.Equal(AccountStage.Evaluation, evt.FromStage);
        Assert.Equal(AccountStage.Funded, evt.ToStage);
        Assert.False(account.IsTerminal);
    }

    [Theory]
    [InlineData(AccountStage.Failed)]
    [InlineData(AccountStage.Withdrawn)]
    [InlineData(AccountStage.Expired)]
    public void TransitionTo_TerminalStage_SetsClosedOnAndIsTerminal(AccountStage terminalStage)
    {
        var account = CreateAccount();
        var occurredAt = new DateTimeOffset(2026, 3, 15, 0, 0, 0, TimeSpan.Zero);

        account.TransitionTo(terminalStage, occurredAt);

        Assert.Equal(terminalStage, account.Stage);
        Assert.Equal(new DateOnly(2026, 3, 15), account.ClosedOn);
        Assert.True(account.IsTerminal);
    }

    [Fact]
    public void TransitionTo_MultipleTransitions_AppendsFullHistory()
    {
        var account = CreateAccount();

        account.TransitionTo(AccountStage.Funded, new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero));
        account.TransitionTo(AccountStage.Failed, new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero), "Violó trailing drawdown");

        Assert.Equal(2, account.Events.Count);
        Assert.Equal(AccountStage.Failed, account.Stage);
        // FundedOn no se sobreescribe una vez fondeada, aunque luego falle.
        Assert.Equal(new DateOnly(2026, 2, 1), account.FundedOn);
        Assert.Equal(new DateOnly(2026, 5, 1), account.ClosedOn);
    }

    [Fact]
    public void Trade_NetPnL_SubtractsCommissionsFromGross()
    {
        var trade = new Trade { GrossPnL = 500m, Commissions = 4.5m };

        Assert.Equal(495.5m, trade.NetPnL);
    }

    [Fact]
    public void Trade_RMultiple_IsNullWithoutRiskedAmount()
    {
        var trade = new Trade { GrossPnL = 200m, Commissions = 0m, RiskedAmount = null };

        Assert.Null(trade.RMultiple);
    }

    [Fact]
    public void Trade_RMultiple_ComputesRatioAgainstRisk()
    {
        var trade = new Trade { GrossPnL = 300m, Commissions = 0m, RiskedAmount = 100m };

        Assert.Equal(3m, trade.RMultiple);
    }
}
