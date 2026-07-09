using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrackRecord.Domain.Entities;

namespace TrackRecord.Infrastructure.Persistence.Configurations;

public class DailyMindsetCheckInConfiguration : IEntityTypeConfiguration<DailyMindsetCheckIn>
{
    public void Configure(EntityTypeBuilder<DailyMindsetCheckIn> builder)
    {
        builder.ToTable("DailyMindsetCheckIns");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
        builder.Property(x => x.Note).HasMaxLength(500);

        builder.HasIndex(x => new { x.UserId, x.Date }).IsUnique();
    }
}
