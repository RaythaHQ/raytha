using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raytha.Domain.Entities;

namespace Raytha.Infrastructure.Persistence.Configurations;

public class NavigationMenuConfiguration : IEntityTypeConfiguration<NavigationMenu>
{
    public void Configure(EntityTypeBuilder<NavigationMenu> builder)
    {
        builder.HasIndex(nm => nm.DeveloperName).IsUnique();

        builder.HasOne(nm => nm.CreatorUser).WithMany().HasForeignKey(nm => nm.CreatorUserId);

        builder
            .HasOne(nm => nm.LastModifierUser)
            .WithMany()
            .HasForeignKey(nm => nm.LastModifierUserId);
    }
}
