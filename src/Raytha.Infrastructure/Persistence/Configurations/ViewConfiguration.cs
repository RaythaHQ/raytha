using Raytha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raytha.Domain.ValueObjects;

namespace Raytha.Infrastructure.Persistence.Configurations;

public class ViewConfiguration : IEntityTypeConfiguration<View>
{
    public void Configure(EntityTypeBuilder<View> builder)
    {
        builder
            .HasMany(p => p.UserFavorites)
            .WithMany(p => p.FavoriteViews);

        builder
            .HasOne(b => b.CreatorUser)
            .WithMany()
            .HasForeignKey(b => b.CreatorUserId);

        builder
            .HasOne(b => b.LastModifierUser)
            .WithMany()
            .HasForeignKey(b => b.LastModifierUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
