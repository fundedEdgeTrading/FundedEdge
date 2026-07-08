using TrackRecord.Infrastructure.Ai;

namespace TrackRecord.Application.Tests;

public class WeeklyAiReportServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IsDue_NoPreviousReport_IsDue() =>
        Assert.True(WeeklyAiReportService.IsDue(null, Now, intervalDays: 7));

    [Fact]
    public void IsDue_ReportOlderThanInterval_IsDue() =>
        Assert.True(WeeklyAiReportService.IsDue(Now.AddDays(-8), Now, intervalDays: 7));

    [Fact]
    public void IsDue_RecentReport_IsNotDue() =>
        Assert.False(WeeklyAiReportService.IsDue(Now.AddDays(-3), Now, intervalDays: 7));

    [Fact]
    public void IsDue_ExactlyAtInterval_IsDue() =>
        Assert.True(WeeklyAiReportService.IsDue(Now.AddDays(-7), Now, intervalDays: 7));
}
