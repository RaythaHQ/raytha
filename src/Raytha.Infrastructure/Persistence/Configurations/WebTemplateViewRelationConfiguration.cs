using Microsoft.EntityFrameworkCore;
using Raytha.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Raytha.Infrastructure.Persistence.Configurations;

public class WebTemplateViewRelationConfiguration : IEntityTypeConfiguration<WebTemplateViewRelation>
{
    public void Configure(EntityTypeBuilder<WebTemplateViewRelation> builder)
    {
        builder
            .HasOne(wtr => wtr.WebTemplate)
            .WithMany()
            .HasForeignKey(wtr => wtr.WebTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(wtr => wtr.View)
            .WithMany()
            .HasForeignKey(wtr => wtr.ViewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(wtr => new { wtr.ViewId, wtr.WebTemplateId })
            .IsUnique();
    }
}