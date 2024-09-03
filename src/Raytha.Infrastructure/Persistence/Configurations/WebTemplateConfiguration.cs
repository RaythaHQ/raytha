using Raytha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Raytha.Infrastructure.Persistence.Configurations;

public class WebTemplateConfiguration : IEntityTypeConfiguration<WebTemplate>
{
    public void Configure(EntityTypeBuilder<WebTemplate> builder)
    {
        builder.HasIndex(b => new { b.DeveloperName, b.ThemeId })
            .IsUnique();

        builder
            .HasOne(b => b.CreatorUser)
            .WithMany()
            .HasForeignKey(b => b.CreatorUserId);

        builder
            .HasOne(b => b.LastModifierUser)
            .WithMany()
            .HasForeignKey(b => b.LastModifierUserId);
    }
}
