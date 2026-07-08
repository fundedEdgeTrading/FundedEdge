using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrackRecord.Domain.Entities;

namespace TrackRecord.Infrastructure.Persistence.Configurations;

public class IntegrationSettingConfiguration : IEntityTypeConfiguration<IntegrationSetting>
{
    public void Configure(EntityTypeBuilder<IntegrationSetting> builder)
    {
        builder.ToTable("IntegrationSettings");
        builder.HasKey(x => x.Key);
        builder.Property(x => x.Key).HasMaxLength(200);
        builder.Property(x => x.ProtectedValue).IsRequired();
    }
}
