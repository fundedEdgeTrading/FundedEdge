using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FundedEdge.Domain.Entities;

namespace FundedEdge.Infrastructure.Persistence.Configurations;

public class ProcessedWebhookEventConfiguration : IEntityTypeConfiguration<ProcessedWebhookEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedWebhookEvent> builder)
    {
        builder.ToTable("ProcessedWebhookEvents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(200);
        builder.Property(x => x.ProcessedAt).IsRequired();
    }
}
