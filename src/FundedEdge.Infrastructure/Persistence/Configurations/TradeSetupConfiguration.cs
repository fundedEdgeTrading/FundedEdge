using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FundedEdge.Domain.Entities;

namespace FundedEdge.Infrastructure.Persistence.Configurations;

public class TradeSetupConfiguration : IEntityTypeConfiguration<TradeSetup>
{
    public void Configure(EntityTypeBuilder<TradeSetup> builder)
    {
        builder.ToTable("TradeSetups");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => new { x.UserId, x.Name }).IsUnique();
    }
}
