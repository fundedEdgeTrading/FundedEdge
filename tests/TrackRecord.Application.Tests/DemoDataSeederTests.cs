using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TrackRecord.Domain.Enums;
using TrackRecord.Infrastructure.Identity;
using TrackRecord.Infrastructure.Persistence;

namespace TrackRecord.Application.Tests;

public class DemoDataSeederTests
{
    private static (DemoDataSeeder Seeder, InMemoryDbContextFactory Factory) Build()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());

        // UserManager mínimo (sin validadores): el hash de contraseña y la normalización de
        // email son lo único que el seeder necesita del stack de Identity.
        var userManager = new UserManager<ApplicationUser>(
            new UserStore<ApplicationUser>(factory.CreateDbContext()),
            optionsAccessor: null!,
            new PasswordHasher<ApplicationUser>(),
            userValidators: [],
            passwordValidators: [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            services: null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);

        return (new DemoDataSeeder(userManager, factory, NullLogger<DemoDataSeeder>.Instance), factory);
    }

    [Fact]
    public async Task SeedAsync_CreatesDemoUserWithFullDataBattery()
    {
        var (seeder, factory) = Build();

        await seeder.SeedAsync();

        await using var db = await factory.CreateDbContextAsync();

        var user = await db.Users.SingleAsync();
        Assert.Equal(DemoDataSeeder.DemoEmail, user.Email);
        Assert.True(user.EmailConfirmed);
        Assert.Equal(PlanTier.Elite, user.PlanTier);

        var accounts = await db.TradingAccounts.Include(a => a.Events).ToListAsync();
        Assert.Equal(6, accounts.Count);
        Assert.All(accounts, a => Assert.Equal(user.Id, a.UserId));

        // Todas las etapas del ciclo de vida están representadas.
        Assert.Contains(accounts, a => a.Stage == AccountStage.Evaluation);
        Assert.Contains(accounts, a => a.Stage == AccountStage.Funded);
        Assert.Contains(accounts, a => a.Stage == AccountStage.Failed);
        Assert.Contains(accounts, a => a.Stage == AccountStage.Withdrawn);

        // Hay un reset (evento Evaluation→Evaluation con su coste) y transiciones auditables.
        Assert.Contains(accounts, a => a.Events.Any(e =>
            e.FromStage == AccountStage.Evaluation && e.ToStage == AccountStage.Evaluation));
        Assert.Contains(await db.AccountCosts.ToListAsync(), c => c.Kind == CostKind.Reset);

        Assert.True(await db.Trades.CountAsync() > 150);
        Assert.Contains(await db.Payouts.ToListAsync(), p => p.Status == PayoutStatus.Paid);
        Assert.Contains(await db.Payouts.ToListAsync(), p => p.Status == PayoutStatus.Requested);
        Assert.True(await db.DailyMindsetCheckIns.CountAsync() >= 8);
        Assert.True(await db.TradeEmotionLogs.CountAsync() >= 20);

        // Hay trades con MAE/MFE y con origen CsvImport (para el badge "verificado" del perfil).
        Assert.Contains(await db.Trades.ToListAsync(), t => t.MaxAdverseExcursion is not null);
        Assert.Contains(await db.Executions.ToListAsync(), e => e.Source == TradeSourceType.CsvImport);
    }

    [Fact]
    public async Task SeedAsync_RunTwice_IsIdempotent()
    {
        var (seeder, factory) = Build();

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        await using var db = await factory.CreateDbContextAsync();
        Assert.Equal(1, await db.Users.CountAsync());
        Assert.Equal(6, await db.TradingAccounts.CountAsync());
    }
}
