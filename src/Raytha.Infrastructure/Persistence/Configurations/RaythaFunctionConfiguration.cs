using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;

namespace Raytha.Infrastructure.Persistence.Configurations;

public class RaythaFunctionConfiguration : IEntityTypeConfiguration<RaythaFunction>
{
    public void Configure(EntityTypeBuilder<RaythaFunction> builder)
    {
        builder.HasIndex(rf => rf.DeveloperName).IsUnique();

        builder.HasOne(rf => rf.CreatorUser).WithMany().HasForeignKey(rf => rf.CreatorUserId);

        builder
            .HasOne(rf => rf.LastModifierUser)
            .WithMany()
            .HasForeignKey(rf => rf.LastModifierUserId);

        builder
            .Property(rf => rf.TriggerType)
            .HasConversion(rft => rft.DeveloperName, v => RaythaFunctionTriggerType.From(v));
    }
}
