using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrackRecord.Domain.Entities;

namespace TrackRecord.Infrastructure.Persistence.Configurations;

public class AccountCostConfiguration : IEntityTypeConfiguration<AccountCost>
{
    public void Configure(EntityTypeBuilder<AccountCost> builder)
    {
        builder.ToTable("AccountCosts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasIndex(x => new { x.AccountId, x.PaidOn });
    }
}
