using Raytha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Raytha.Infrastructure.Persistence.Configurations;

public class OneTimePasswordConfiguration : IEntityTypeConfiguration<OneTimePassword>
{
    public void Configure(EntityTypeBuilder<OneTimePassword> builder)
    {
        builder
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
