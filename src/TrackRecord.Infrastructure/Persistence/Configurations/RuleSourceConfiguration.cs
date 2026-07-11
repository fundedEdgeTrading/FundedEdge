using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrackRecord.Domain.Entities;

namespace TrackRecord.Infrastructure.Persistence.Configurations;

public class RuleSourceConfiguration : IEntityTypeConfiguration<RuleSource>
{
    public void Configure(EntityTypeBuilder<RuleSource> builder)
    {
        builder.ToTable("RuleSources");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Url).IsRequired().HasMaxLength(500);
        builder.Property(x => x.LastContentHash).HasMaxLength(64); // SHA-256 en hex
        builder.Property(x => x.LastError).HasMaxLength(1000);

        builder.HasOne(x => x.PropFirm)
            .WithMany()
            .HasForeignKey(x => x.PropFirmId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.PropFirmId, x.IsEnabled });
    }
}
