using Raytha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raytha.Domain.ValueObjects.FieldTypes;

namespace Raytha.Infrastructure.Persistence.Configurations;

public class ContentTypeFieldConfiguration : IEntityTypeConfiguration<ContentTypeField>
{
    public void Configure(EntityTypeBuilder<ContentTypeField> builder)
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
            .Property(b => b.FieldType)
            .HasConversion(v => v.DeveloperName, v => BaseFieldType.From(v));

        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}
