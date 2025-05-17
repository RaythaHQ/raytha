using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;

namespace Raytha.Infrastructure.Persistence.Configurations;

public class AuthenticationSchemeConfiguration : IEntityTypeConfiguration<AuthenticationScheme>
{
    public void Configure(EntityTypeBuilder<AuthenticationScheme> builder)
    {
        builder.HasOne(b => b.CreatorUser).WithMany().HasForeignKey(b => b.CreatorUserId);

        builder.HasOne(b => b.LastModifierUser).WithMany().HasForeignKey(b => b.LastModifierUserId);

        builder
            .Property(b => b.AuthenticationSchemeType)
            .HasConversion(v => v.DeveloperName, v => AuthenticationSchemeType.From(v));

        builder.HasIndex(b => b.DeveloperName).IsUnique();
    }
}
