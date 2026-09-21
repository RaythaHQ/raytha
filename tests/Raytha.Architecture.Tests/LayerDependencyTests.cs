using FluentAssertions;

namespace Raytha.Architecture.Tests;

/// <summary>
/// Clean-architecture direction: Web -> Infrastructure -> Application -> Domain.
/// Inner layers must never reference outer ones, and infrastructure concerns (EF provider,
/// ASP.NET Core) must not leak inward.
/// </summary>
[TestFixture]
public class LayerDependencyTests
{
    [Test]
    public void Domain_does_not_reference_outer_layers()
    {
        Layers.Domain.References(Layers.Application).Should().BeFalse();
        Layers.Domain.References(Layers.Infrastructure).Should().BeFalse();
        Layers.Domain.References(Layers.Web).Should().BeFalse();
    }

    [Test]
    public void Domain_does_not_depend_on_persistence_or_hosting_frameworks()
    {
        Layers.Domain.ReferencesAnyStartingWith("Microsoft.EntityFrameworkCore").Should().BeFalse();
        Layers.Domain.ReferencesAnyStartingWith("Npgsql").Should().BeFalse();
        Layers.Domain.ReferencesAnyStartingWith("Microsoft.AspNetCore").Should().BeFalse();
    }

    [Test]
    public void Application_does_not_reference_infrastructure_or_web()
    {
        Layers.Application.References(Layers.Infrastructure).Should().BeFalse();
        Layers.Application.References(Layers.Web).Should().BeFalse();
    }

    [Test]
    public void Application_does_not_depend_on_a_concrete_database_provider()
    {
        // EF Core abstractions are allowed (IRaythaDbContext exposes DbSet<T>); the Npgsql
        // provider and Dapper are infrastructure-only.
        Layers.Application.ReferencesAnyStartingWith("Npgsql").Should().BeFalse();
        Layers.Application.ReferencesAnyStartingWith("Dapper").Should().BeFalse();
    }

    [Test]
    public void Application_does_not_depend_on_aspnetcore()
    {
        Layers.Application.ReferencesAnyStartingWith("Microsoft.AspNetCore").Should().BeFalse();
    }

    [Test]
    public void Infrastructure_does_not_reference_web()
    {
        Layers.Infrastructure.References(Layers.Web).Should().BeFalse();
    }

    [Test]
    public void Infrastructure_references_application_and_domain()
    {
        Layers.Infrastructure.References(Layers.Application).Should().BeTrue();
        Layers.Infrastructure.References(Layers.Domain).Should().BeTrue();
    }

    [Test]
    public void Observability_packages_live_only_in_the_web_host()
    {
        foreach (var layer in new[] { Layers.Domain, Layers.Application, Layers.Infrastructure })
        {
            layer.ReferencesAnyStartingWith("Serilog").Should().BeFalse(layer.GetName().Name);
            layer.ReferencesAnyStartingWith("OpenTelemetry").Should().BeFalse(layer.GetName().Name);
            layer.ReferencesAnyStartingWith("Sentry").Should().BeFalse(layer.GetName().Name);
        }
    }
}
