using FundedEdge.Infrastructure.Email;

namespace FundedEdge.Application.Tests;

public class TradeImportReminderServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 13, 12, 0, 0, TimeSpan.Zero);
    private const int AfterDays = 7;

    [Fact]
    public void NotDue_WhenActivityIsRecent()
    {
        var lastTrade = Now.AddDays(-3);
        Assert.False(TradeImportReminderService.IsReminderDue(lastTrade, null, Now, AfterDays));
    }

    [Fact]
    public void Due_WhenInactiveAndNeverReminded()
    {
        var lastTrade = Now.AddDays(-10);
        Assert.True(TradeImportReminderService.IsReminderDue(lastTrade, null, Now, AfterDays));
    }

    [Fact]
    public void NotDue_WhenAlreadyRemindedInsideCurrentWindow()
    {
        var lastTrade = Now.AddDays(-10);
        var lastReminder = Now.AddDays(-2); // posterior al último trade y dentro de la ventana
        Assert.False(TradeImportReminderService.IsReminderDue(lastTrade, lastReminder, Now, AfterDays));
    }

    [Fact]
    public void Due_Again_WhenReminderIsOlderThanWindow()
    {
        var lastTrade = Now.AddDays(-20);
        var lastReminder = Now.AddDays(-8); // ya recordado, pero hace más de una ventana
        Assert.True(TradeImportReminderService.IsReminderDue(lastTrade, lastReminder, Now, AfterDays));
    }

    [Fact]
    public void Due_WhenUserImportedAfterLastReminderAndWentQuietAgain()
    {
        var lastReminder = Now.AddDays(-30);
        var lastTrade = Now.AddDays(-9); // importó tras el último recordatorio y volvió a parar
        Assert.True(TradeImportReminderService.IsReminderDue(lastTrade, lastReminder, Now, AfterDays));
    }
}
