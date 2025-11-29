using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raytha.Domain.Entities;

namespace Raytha.Infrastructure.Persistence.Configurations;

public class SitePageConfiguration : IEntityTypeConfiguration<SitePage>
{
    public void Configure(EntityTypeBuilder<SitePage> builder)
    {
        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);

        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);

        builder.HasOne(b => b.WebTemplate).WithMany().HasForeignKey(b => b.WebTemplateId);

        builder.Property(b => b._DraftWidgetsJson).HasColumnName("_DraftWidgetsJson");
        builder.Property(b => b._PublishedWidgetsJson).HasColumnName("_PublishedWidgetsJson");
    }
}

public class SitePagePostgresConfiguration
    : IEntityTypeConfiguration<SitePage>,
        IPostgresConfiguration
{
    public void Configure(EntityTypeBuilder<SitePage> builder)
    {
        builder.Property(b => b._DraftWidgetsJson).HasColumnType("jsonb");
        builder.Property(b => b._PublishedWidgetsJson).HasColumnType("jsonb");
    }
}

public class SitePageRevisionConfiguration : IEntityTypeConfiguration<SitePageRevision>
{
    public void Configure(EntityTypeBuilder<SitePageRevision> builder)
    {
        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);

        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);

        builder.HasOne(b => b.SitePage).WithMany().HasForeignKey(b => b.SitePageId);

        builder.Property(b => b._PublishedWidgetsJson).HasColumnName("_PublishedWidgetsJson");
    }
}

public class SitePageRevisionPostgresConfiguration
    : IEntityTypeConfiguration<SitePageRevision>,
        IPostgresConfiguration
{
    public void Configure(EntityTypeBuilder<SitePageRevision> builder)
    {
        builder.Property(b => b._PublishedWidgetsJson).HasColumnType("jsonb");
    }
}
