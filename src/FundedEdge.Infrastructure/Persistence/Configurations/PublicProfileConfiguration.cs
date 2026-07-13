using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FundedEdge.Domain.Entities;

namespace FundedEdge.Infrastructure.Persistence.Configurations;

public class PublicProfileConfiguration : IEntityTypeConfiguration<PublicProfile>
{
    public void Configure(EntityTypeBuilder<PublicProfile> builder)
    {
        builder.ToTable("PublicProfiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(60);
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasIndex(x => x.Slug).IsUnique();
    }
}
