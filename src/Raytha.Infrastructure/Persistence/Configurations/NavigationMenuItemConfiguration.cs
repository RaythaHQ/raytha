using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raytha.Domain.Entities;

namespace Raytha.Infrastructure.Persistence.Configurations;

public class NavigationMenuItemConfiguration : IEntityTypeConfiguration<NavigationMenuItem>
{
    public void Configure(EntityTypeBuilder<NavigationMenuItem> builder)
    {
        builder
            .HasOne(nmi => nmi.ParentNavigationMenuItem)
            .WithMany()
            .HasForeignKey(nmi => nmi.ParentNavigationMenuItemId)
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.HasOne(nm => nm.CreatorUser).WithMany().HasForeignKey(nm => nm.CreatorUserId);

        builder
            .HasOne(nm => nm.LastModifierUser)
            .WithMany()
            .HasForeignKey(nm => nm.LastModifierUserId);
    }
}
