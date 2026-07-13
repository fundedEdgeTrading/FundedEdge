using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FundedEdge.Domain.Entities;

namespace FundedEdge.Infrastructure.Persistence.Configurations;

public class TradeEmotionLogConfiguration : IEntityTypeConfiguration<TradeEmotionLog>
{
    public void Configure(EntityTypeBuilder<TradeEmotionLog> builder)
    {
        builder.ToTable("TradeEmotionLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Note).HasMaxLength(500);

        builder.HasOne(x => x.Trade)
            .WithMany()
            .HasForeignKey(x => x.TradeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.TradeId);
    }
}
