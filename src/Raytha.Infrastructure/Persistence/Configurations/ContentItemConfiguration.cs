using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raytha.Domain.Entities;

namespace Raytha.Infrastructure.Persistence.Configurations;

public class ContentItemConfiguration : IEntityTypeConfiguration<ContentItem>
{
    public void Configure(EntityTypeBuilder<ContentItem> builder)
    {
        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);

        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);
    }
}

public class ContentItemPostgresConfiguration
    : IEntityTypeConfiguration<ContentItem>,
        IPostgresConfiguration
{
    public void Configure(EntityTypeBuilder<ContentItem> builder)
    {
        builder.Property(b => b._PublishedContent).HasColumnType("jsonb");
    }
}
