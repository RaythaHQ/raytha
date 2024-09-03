using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raytha.Domain.Entities;

namespace Raytha.Infrastructure.Persistence.Configurations;

public class ThemeConfiguration : IEntityTypeConfiguration<Theme>
{
    public void Configure(EntityTypeBuilder<Theme> builder)
    {
        builder
            .HasIndex(t => t.DeveloperName)
            .IsUnique();

        builder
            .HasOne(t => t.CreatorUser)
            .WithMany()
            .HasForeignKey(t => t.CreatorUserId);

        builder
            .HasOne(t => t.LastModifierUser)
            .WithMany()
            .HasForeignKey(t => t.LastModifierUserId);

        builder
            .HasMany(t => t.WebTemplates)
            .WithOne(wt => wt.Theme)
            .HasForeignKey(wt => wt.ThemeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(t => t.ThemeAccessToMediaItems)
            .WithOne(mi => mi.Theme)
            .HasForeignKey(mi => mi.ThemeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}