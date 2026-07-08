using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrackRecord.Domain.Entities;

namespace TrackRecord.Infrastructure.Persistence.Configurations;

public class ExecutionConfiguration : IEntityTypeConfiguration<Execution>
{
    public void Configure(EntityTypeBuilder<Execution> builder)
    {
        builder.ToTable("Executions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ExternalId).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Symbol).IsRequired().HasMaxLength(30);
        builder.Property(x => x.Price).HasColumnType("decimal(18,8)");
        builder.Property(x => x.Commission).HasColumnType("decimal(18,4)");

        // Idempotencia al importar: la combinación (Source, ExternalId) es única.
        builder.HasIndex(x => new { x.Source, x.ExternalId }).IsUnique();
        builder.HasIndex(x => new { x.AccountId, x.ExecutedAt });

        // Evita ciclos de cascada: Execution → Account es Restrict, no Cascade.
        // Las cascadas vienen via Trade → Account. No permite borrar una cuenta si tiene fills orphans.
        builder.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
