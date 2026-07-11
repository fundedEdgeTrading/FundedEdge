using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrackRecord.Domain.Entities;

namespace TrackRecord.Infrastructure.Persistence.Configurations;

public class ProposedProgramChangeConfiguration : IEntityTypeConfiguration<ProposedProgramChange>
{
    public void Configure(EntityTypeBuilder<ProposedProgramChange> builder)
    {
        builder.ToTable("ProposedProgramChanges");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProgramName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.SourceUrl).HasMaxLength(500);
        builder.Property(x => x.PayloadJson).IsRequired();

        builder.HasOne(x => x.PropFirm)
            .WithMany()
            .HasForeignKey(x => x.PropFirmId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.Status);
    }
}
