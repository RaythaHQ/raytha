using Microsoft.EntityFrameworkCore;
using Raytha.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Raytha.Infrastructure.Persistence.Configurations;

public class RaythaFunctionRevisionConfiguration : IEntityTypeConfiguration<RaythaFunctionRevision>
{
    public void Configure(EntityTypeBuilder<RaythaFunctionRevision> builder)
    {
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
