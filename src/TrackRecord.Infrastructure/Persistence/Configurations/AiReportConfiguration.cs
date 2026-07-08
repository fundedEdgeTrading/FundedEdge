using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrackRecord.Domain.Entities;

namespace TrackRecord.Infrastructure.Persistence.Configurations;

public class AiReportConfiguration : IEntityTypeConfiguration<AiReport>
{
    public void Configure(EntityTypeBuilder<AiReport> builder)
    {
        builder.ToTable("AiReports");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(450);
        builder.Property(x => x.Question).HasMaxLength(2000);
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.Model).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.UserId);
    }
}
