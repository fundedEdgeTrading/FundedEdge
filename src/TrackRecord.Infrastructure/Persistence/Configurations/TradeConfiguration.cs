using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrackRecord.Domain.Entities;

namespace TrackRecord.Infrastructure.Persistence.Configurations;

public class TradeConfiguration : IEntityTypeConfiguration<Trade>
{
    public void Configure(EntityTypeBuilder<Trade> builder)
    {
        builder.ToTable("Trades");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Symbol).IsRequired().HasMaxLength(30);
        builder.Property(x => x.AvgEntryPrice).HasColumnType("decimal(18,8)");
        builder.Property(x => x.AvgExitPrice).HasColumnType("decimal(18,8)");
        builder.Property(x => x.GrossPnL).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Commissions).HasColumnType("decimal(18,2)");
        builder.Property(x => x.RiskedAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Tags).HasMaxLength(300);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Ignore(x => x.NetPnL);
        builder.Ignore(x => x.RMultiple);

        builder.HasOne(x => x.Instrument)
            .WithMany()
            .HasForeignKey(x => x.InstrumentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Executions)
            .WithOne(x => x.Trade)
            .HasForeignKey(x => x.TradeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.AccountId, x.ClosedAt });
    }
}
