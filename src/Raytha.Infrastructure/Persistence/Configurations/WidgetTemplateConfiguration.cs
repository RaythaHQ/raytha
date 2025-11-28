using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raytha.Domain.Entities;

namespace Raytha.Infrastructure.Persistence.Configurations;

public class WidgetTemplateConfiguration : IEntityTypeConfiguration<WidgetTemplate>
{
    public void Configure(EntityTypeBuilder<WidgetTemplate> builder)
    {
        builder.HasIndex(b => new { b.DeveloperName, b.ThemeId }).IsUnique();

        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);

        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);

        builder.HasOne(b => b.Theme).WithMany().HasForeignKey(b => b.ThemeId);
    }
}

public class WidgetTemplateRevisionConfiguration : IEntityTypeConfiguration<WidgetTemplateRevision>
{
    public void Configure(EntityTypeBuilder<WidgetTemplateRevision> builder)
    {
        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);

        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);

        builder
            .HasOne(b => b.WidgetTemplate)
            .WithMany(w => w.Revisions)
            .HasForeignKey(b => b.WidgetTemplateId);
    }
}

