using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Common;
using Raytha.Infrastructure.Persistence;

namespace Raytha.Architecture.Tests;

[TestFixture]
public class PersistenceConventionTests
{
    [Test]
    public void Every_DbSet_on_the_application_interface_is_implemented_by_the_context()
    {
        var interfaceSets = typeof(IRaythaDbContext)
            .GetProperties()
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0])
            .ToList();

        var contextSets = typeof(RaythaDbContext)
            .GetProperties()
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0])
            .ToHashSet();

        interfaceSets.Should().NotBeEmpty();
        interfaceSets.Should().OnlyContain(t => contextSets.Contains(t));
    }

    [Test]
    public void Phase2_entities_are_exposed_on_the_application_interface()
    {
        var exposed = typeof(IRaythaDbContext)
            .GetProperties()
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0])
            .ToHashSet();

        exposed.Should().Contain(typeof(Raytha.Domain.Entities.Webhook));
        exposed.Should().Contain(typeof(Raytha.Domain.Entities.WebhookDelivery));
        exposed.Should().Contain(typeof(Raytha.Domain.Entities.EmailLog));
    }

    [Test]
    public void Every_entity_type_configuration_targets_a_domain_entity()
    {
        var configurations = Layers
            .Infrastructure.SafeGetTypes()
            .SelectMany(t => t.GetInterfaces())
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>))
            .Select(i => i.GetGenericArguments()[0])
            .ToList();

        configurations.Should().NotBeEmpty();
        configurations.Should().OnlyContain(t => t.Assembly == Layers.Domain && typeof(IBaseEntity).IsAssignableFrom(t));
    }

    [Test]
    public void Migrations_include_v2_0_0()
    {
        Layers
            .Infrastructure.SafeGetTypes()
            .Where(t => typeof(Microsoft.EntityFrameworkCore.Migrations.Migration).IsAssignableFrom(t))
            .Select(t => t.Name)
            .Should()
            .Contain("v2_0_0");
    }
}
