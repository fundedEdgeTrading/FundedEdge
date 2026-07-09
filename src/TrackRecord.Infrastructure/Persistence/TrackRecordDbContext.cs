using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TrackRecord.Domain.Entities;
using TrackRecord.Infrastructure.Identity;

namespace TrackRecord.Infrastructure.Persistence;

public class TrackRecordDbContext(DbContextOptions<TrackRecordDbContext> options)
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TrackRecordDbContext).Assembly);
        SeedData.Apply(modelBuilder);
    }
}
