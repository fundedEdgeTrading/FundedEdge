using Microsoft.EntityFrameworkCore;
using FundedEdge.Application.Abstractions;
using FundedEdge.Domain.Entities;
using FundedEdge.Domain.Enums;
using FundedEdge.Infrastructure.Identity;
using FundedEdge.Infrastructure.Services;

namespace FundedEdge.Application.Tests;

public class PlanServiceTests
{
    private const string UserId = "user-1";

    private static async Task<InMemoryDbContextFactory> SeedUserAsync(PlanTier tier, DateTimeOffset? trialEndsAt = null)
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        await using var db = await factory.CreateDbContextAsync();
        db.Users.Add(new ApplicationUser { Id = UserId, UserName = "user@test.com", PlanTier = tier, TrialEndsAt = trialEndsAt });
        await db.SaveChangesAsync();
        return factory;
    }

    private static async Task SeedAccountAsync(InMemoryDbContextFactory factory, AccountStage stage)
    {
        await using var db = await factory.CreateDbContextAsync();
        var propFirmId = Guid.NewGuid();
        db.PropFirms.Add(new PropFirm { Id = propFirmId, Name = "Test Firm" });
        db.TradingAccounts.Add(new TradingAccount
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            PropFirmId = propFirmId,
            DisplayName = "Acc",
            AccountSize = 50_000,
            PurchasedOn = new DateOnly(2026, 1, 1),
            Stage = stage,
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedAiReportAsync(InMemoryDbContextFactory factory, AiReportKind kind, DateTimeOffset createdAt)
    {
        await using var db = await factory.CreateDbContextAsync();
        db.AiReports.Add(new AiReport
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            Kind = kind,
            Content = "contenido de prueba",
            CreatedAt = createdAt,
            Model = "claude-haiku-4-5",
        });
        await db.SaveChangesAsync();
    }

    private static PlanService BuildService(InMemoryDbContextFactory factory) =>
        new(factory, new FakeCurrentUserAccessor(UserId));

    [Fact]
    public async Task Starter_With2ActiveAccounts_CannotCreateAThird()
    {
        var factory = await SeedUserAsync(PlanTier.Starter);
        await SeedAccountAsync(factory, AccountStage.Evaluation);
        await SeedAccountAsync(factory, AccountStage.Funded);

        Assert.False(await BuildService(factory).CanCreateAccountAsync());
    }

    [Fact]
    public async Task Pro_With9ActiveAccounts_CanCreateUpToTen()
    {
        var factory = await SeedUserAsync(PlanTier.Pro);
        for (var i = 0; i < 9; i++) await SeedAccountAsync(factory, AccountStage.Evaluation);

        Assert.True(await BuildService(factory).CanCreateAccountAsync());
    }

    [Fact]
    public async Task Elite_HasNoAccountLimit()
    {
        var factory = await SeedUserAsync(PlanTier.Elite);
        for (var i = 0; i < 11; i++) await SeedAccountAsync(factory, AccountStage.Evaluation);

        Assert.True(await BuildService(factory).CanCreateAccountAsync());
    }

    [Fact]
    public async Task TerminalAccounts_DoNotCountTowardsTheLimit()
    {
        var factory = await SeedUserAsync(PlanTier.Starter);
        await SeedAccountAsync(factory, AccountStage.Failed);
        await SeedAccountAsync(factory, AccountStage.Withdrawn);
        await SeedAccountAsync(factory, AccountStage.Expired);

        Assert.True(await BuildService(factory).CanCreateAccountAsync());
    }

    [Fact]
    public async Task ActiveTrial_IsTreatedAsPro()
    {
        var factory = await SeedUserAsync(PlanTier.Starter, trialEndsAt: DateTimeOffset.UtcNow.AddDays(3));

        var limits = await BuildService(factory).GetLimitsAsync();

        Assert.Equal(PlanLimits.For(PlanTier.Pro), limits);
    }

    [Fact]
    public async Task ExpiredTrial_FallsBackToStarter()
    {
        var factory = await SeedUserAsync(PlanTier.Starter, trialEndsAt: DateTimeOffset.UtcNow.AddDays(-1));

        var tier = await BuildService(factory).GetTierAsync();

        Assert.Equal(PlanTier.Starter, tier);
    }

    [Fact]
    public async Task PaidPlan_IsNotOverriddenByALeftoverTrialDate()
    {
        // Un usuario que ya pagó Elite conserva su tier aunque TrialEndsAt siga en el futuro
        // (p. ej. quedó de cuando probaba Pro antes de subir de plan).
        var factory = await SeedUserAsync(PlanTier.Elite, trialEndsAt: DateTimeOffset.UtcNow.AddDays(3));

        var tier = await BuildService(factory).GetTierAsync();

        Assert.Equal(PlanTier.Elite, tier);
    }

    [Fact]
    public async Task UnknownUser_DefaultsToStarterLimits()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());

        var tier = await BuildService(factory).GetTierAsync();

        Assert.Equal(PlanTier.Starter, tier);
    }

    [Fact]
    public async Task Starter_With1ReportThisMonth_CannotGenerateAnotherAndSuggestsUpgrade()
    {
        var factory = await SeedUserAsync(PlanTier.Starter);
        await SeedAiReportAsync(factory, AiReportKind.Analysis, DateTimeOffset.UtcNow.AddDays(-2));

        var allowance = await BuildService(factory).GetAiAllowanceAsync();

        Assert.False(allowance.CanGenerateReport);
    }

    [Fact]
    public async Task Pro_ReportOlderThanAWeek_CanGenerateAnother()
    {
        var factory = await SeedUserAsync(PlanTier.Pro);
        await SeedAiReportAsync(factory, AiReportKind.Analysis, DateTimeOffset.UtcNow.AddDays(-8));

        var allowance = await BuildService(factory).GetAiAllowanceAsync();

        Assert.True(allowance.CanGenerateReport);
    }

    [Fact]
    public async Task Pro_ReportTwoDaysAgo_CannotGenerateAnotherWithinTheWeek()
    {
        var factory = await SeedUserAsync(PlanTier.Pro);
        await SeedAiReportAsync(factory, AiReportKind.Analysis, DateTimeOffset.UtcNow.AddDays(-2));

        var allowance = await BuildService(factory).GetAiAllowanceAsync();

        Assert.False(allowance.CanGenerateReport);
    }

    [Fact]
    public async Task Questions_AreCountedSeparatelyFromReports()
    {
        var factory = await SeedUserAsync(PlanTier.Pro);
        await SeedAiReportAsync(factory, AiReportKind.Analysis, DateTimeOffset.UtcNow.AddHours(-1)); // agota el cupo de informes

        var allowance = await BuildService(factory).GetAiAllowanceAsync();

        Assert.False(allowance.CanGenerateReport);
        Assert.True(allowance.CanAskQuestion); // el cupo de preguntas es independiente
        Assert.Equal(0, allowance.QuestionsUsed);
    }

    [Fact]
    public async Task Elite_RespectsTheDailyHardCapEvenWithUnlimitedQuestions()
    {
        var factory = await SeedUserAsync(PlanTier.Elite);
        var limits = PlanLimits.For(PlanTier.Elite);
        for (var i = 0; i < limits.AiDailyHardCap; i++)
        {
            await SeedAiReportAsync(factory, AiReportKind.AdHocQuestion, DateTimeOffset.UtcNow);
        }

        var allowance = await BuildService(factory).GetAiAllowanceAsync();

        Assert.Null(allowance.QuestionsLimit); // "ilimitadas" de cara al usuario...
        Assert.False(allowance.CanAskQuestion); // ...pero el tope anti-abuso diario sigue aplicando
        Assert.False(allowance.CanGenerateReport);
    }

    [Theory]
    [InlineData(PlanTier.Starter, "claude-haiku-4-5", "Low")]
    [InlineData(PlanTier.Pro, "claude-haiku-4-5", "Medium")]
    [InlineData(PlanTier.Elite, "claude-opus-4-8", "High")]
    public void PlanLimits_SelectsTheModelAndEffortDocumentedForEachTier(PlanTier tier, string expectedModel, string expectedEffort)
    {
        var limits = PlanLimits.For(tier);

        Assert.Equal(expectedModel, limits.AiModelId);
        Assert.Equal(expectedEffort, limits.AiEffort);
    }
}
