using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raytha.Domain.Entities;

namespace Raytha.Infrastructure.Persistence.Configurations;

public class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
    {
        builder
            .HasOne(b => b.Webhook)
            .WithMany(b => b.Deliveries)
            .HasForeignKey(b => b.WebhookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Property(b => b.Status)
            .HasConversion(v => v.DeveloperName, v => WebhookDeliveryStatus.From(v));

        builder.HasIndex(b => b.WebhookId);
        builder.HasIndex(b => b.CreationTime);
        builder.HasIndex(b => b.EventName);
    }
}
