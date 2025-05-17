using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raytha.Domain.Entities;

namespace Raytha.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder
            .HasOne(p => p.AuthenticationScheme)
            .WithMany()
            .HasForeignKey(p => p.AuthenticationSchemeId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);

        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);

        builder.HasIndex(b => b.EmailAddress).IsUnique();
        builder.HasIndex(b => new { b.SsoId, b.AuthenticationSchemeId }).IsUnique();
    }
}
