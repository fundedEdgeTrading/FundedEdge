using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FundedEdge.Domain.Entities;

namespace FundedEdge.Infrastructure.Persistence.Configurations;

public class PropFirmConfiguration : IEntityTypeConfiguration<PropFirm>
{
    public void Configure(EntityTypeBuilder<PropFirm> builder)
    {
        builder.ToTable("PropFirms");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Website).HasMaxLength(300);
        builder.Property(x => x.Country).HasMaxLength(100);
        builder.Property(x => x.HealthNotes).HasMaxLength(1000);
        builder.Property(x => x.RulesSource).HasMaxLength(50);
        // RulesMarkdown / RulesSourceUrls sin longitud máxima → columna "text".
        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasMany(x => x.Accounts)
            .WithOne(x => x.PropFirm)
            .HasForeignKey(x => x.PropFirmId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
