using Raytha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Raytha.Infrastructure.Persistence.Configurations;

public class RouteConfiguration : IEntityTypeConfiguration<Route>
{
    public void Configure(EntityTypeBuilder<Route> builder)
    {
        builder
            .HasOne(p => p.View)
            .WithOne(p => p.Route)
            .HasForeignKey<View>(b => b.RouteId)
            .OnDelete(DeleteBehavior.ClientCascade);

        builder
            .HasOne(p => p.ContentItem)
            .WithOne(p => p.Route)
            .HasForeignKey<ContentItem>(b => b.RouteId)
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.HasIndex(b => b.Path).IsUnique();
    }
}