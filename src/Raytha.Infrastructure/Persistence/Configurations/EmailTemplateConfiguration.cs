using Raytha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raytha.Domain.ValueObjects;

namespace Raytha.Infrastructure.Persistence.Configurations;

public class EmailTemplateConfiguration : IEntityTypeConfiguration<WebTemplate>
{
    public void Configure(EntityTypeBuilder<WebTemplate> builder)
    {
        builder
            .HasIndex(b => b.DeveloperName)
            .IsUnique()
            .IncludeProperties(p => new { p.Id, p.Label });

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
