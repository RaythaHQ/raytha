using Raytha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Raytha.Infrastructure.Persistence.Configurations;

public class ContentTypeConfiguration : IEntityTypeConfiguration<ContentType>
{
    public void Configure(EntityTypeBuilder<ContentType> builder)
    {
        builder
            .HasMany(p => p.ContentTypeFields)
            .WithOne(p => p.ContentType)
            .HasForeignKey(p => p.ContentTypeId)
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
