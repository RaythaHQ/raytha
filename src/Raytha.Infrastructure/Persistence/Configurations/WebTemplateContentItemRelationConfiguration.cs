using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raytha.Domain.Entities;

namespace Raytha.Infrastructure.Persistence.Configurations;

public class WebTemplateContentItemRelationConfiguration : IEntityTypeConfiguration<WebTemplateContentItemRelation>
{
    public void Configure(EntityTypeBuilder<WebTemplateContentItemRelation> builder)
    {
        builder
            .HasOne(wtr => wtr.WebTemplate)
            .WithMany()
            .HasForeignKey(wtr => wtr.WebTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(wtr => wtr.ContentItem)
            .WithMany()
            .HasForeignKey(wtr => wtr.ContentItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(wtr => new { wtr.WebTemplateId, wtr.ContentItemId })
            .IsUnique();
    }
}