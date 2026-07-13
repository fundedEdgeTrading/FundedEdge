using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FundedEdge.Domain.Entities;

namespace FundedEdge.Infrastructure.Persistence.Configurations;

public class PayoutConfiguration : IEntityTypeConfiguration<Payout>
{
    public void Configure(EntityTypeBuilder<Payout> builder)
    {
        builder.ToTable("Payouts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AmountRequested).HasColumnType("decimal(18,2)");
        builder.Property(x => x.AmountReceived).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasIndex(x => new { x.AccountId, x.RequestedOn });
    }
}
