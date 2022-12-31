using Raytha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Raytha.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder
            .HasIndex(b => b.EmailAddress)
            .IsUnique()
            .IncludeProperties(p => new { p.Id, p.FirstName, p.LastName, p.SsoId, p.AuthenticationSchemeId });

        builder
            .HasIndex(b => new { b.SsoId, b.AuthenticationSchemeId })
            .IsUnique()
            .IncludeProperties(p => new { p.Id, p.EmailAddress, p.FirstName, p.LastName });

        builder
            .HasOne(p => p.AuthenticationScheme)
            .WithMany()
            .HasForeignKey(p => p.AuthenticationSchemeId)
            .OnDelete(DeleteBehavior.NoAction);

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
