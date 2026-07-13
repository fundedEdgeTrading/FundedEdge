using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FundedEdge.Domain.Entities;
using FundedEdge.Infrastructure.Identity;

namespace FundedEdge.Infrastructure.Persistence;

public class FundedEdgeDbContext(DbContextOptions<FundedEdgeDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<PropFirm> PropFirms => Set<PropFirm>();
    public DbSet<EvaluationProgram> EvaluationPrograms => Set<EvaluationProgram>();
    public DbSet<TradingAccount> TradingAccounts => Set<TradingAccount>();
    public DbSet<AccountEvent> AccountEvents => Set<AccountEvent>();
    public DbSet<AccountCost> AccountCosts => Set<AccountCost>();
    public DbSet<Payout> Payouts => Set<Payout>();
    public DbSet<Instrument> Instruments => Set<Instrument>();
    public DbSet<Trade> Trades => Set<Trade>();
    public DbSet<Execution> Executions => Set<Execution>();
    public DbSet<AiReport> AiReports => Set<AiReport>();
    public DbSet<IntegrationSetting> IntegrationSettings => Set<IntegrationSetting>();
    public DbSet<PublicProfile> PublicProfiles => Set<PublicProfile>();
    public DbSet<ProcessedWebhookEvent> ProcessedWebhookEvents => Set<ProcessedWebhookEvent>();
    public DbSet<TradeEmotionLog> TradeEmotionLogs => Set<TradeEmotionLog>();
    public DbSet<DailyMindsetCheckIn> DailyMindsetCheckIns => Set<DailyMindsetCheckIn>();
    public DbSet<TradeSetup> TradeSetups => Set<TradeSetup>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FundedEdgeDbContext).Assembly);
        SeedData.Apply(modelBuilder);
    }
}
