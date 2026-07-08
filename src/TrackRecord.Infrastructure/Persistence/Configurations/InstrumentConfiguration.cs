using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrackRecord.Domain.Entities;

namespace TrackRecord.Infrastructure.Persistence.Configurations;

public class InstrumentConfiguration : IEntityTypeConfiguration<Instrument>
{
    public void Configure(EntityTypeBuilder<Instrument> builder)
    {
        builder.ToTable("Instruments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Root).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.TickSize).HasColumnType("decimal(18,8)");
        builder.Property(x => x.TickValue).HasColumnType("decimal(18,4)");
        builder.HasIndex(x => x.Root).IsUnique();
    }
}
