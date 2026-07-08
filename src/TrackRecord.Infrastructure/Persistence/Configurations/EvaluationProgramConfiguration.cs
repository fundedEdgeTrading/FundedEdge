using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrackRecord.Domain.Entities;

namespace TrackRecord.Infrastructure.Persistence.Configurations;

public class EvaluationProgramConfiguration : IEntityTypeConfiguration<EvaluationProgram>
{
    public void Configure(EntityTypeBuilder<EvaluationProgram> builder)
    {
        builder.ToTable("EvaluationPrograms");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);

        builder.Property(x => x.AccountSize).HasColumnType("decimal(18,2)");
        builder.Property(x => x.EvaluationCost).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ActivationCost).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ProfitTarget).HasColumnType("decimal(18,2)");
        builder.Property(x => x.MaxDrawdown).HasColumnType("decimal(18,2)");
        builder.Property(x => x.DailyLossLimit).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ConsistencyMaxDayFraction).HasColumnType("decimal(5,4)");

        builder.HasOne(x => x.PropFirm)
            .WithMany()
            .HasForeignKey(x => x.PropFirmId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.PropFirmId, x.IsActive });
    }
}
