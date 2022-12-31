using Raytha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raytha.Domain.ValueObjects;

namespace Raytha.Infrastructure.Persistence.Configurations;

public class VerificationCodeConfiguration : IEntityTypeConfiguration<VerificationCode>
{
    public void Configure(EntityTypeBuilder<VerificationCode> builder)
    {
        builder
            .HasOne(b => b.CreatorUser)
            .WithMany()
            .HasForeignKey(b => b.CreatorUserId);

        builder
            .HasOne(b => b.LastModifierUser)
            .WithMany()
            .HasForeignKey(b => b.LastModifierUserId);

        builder
            .Property(b => b.VerificationCodeType)
            .HasConversion(v => v.DeveloperName, v => VerificationCodeType.From(v));
    }
}
