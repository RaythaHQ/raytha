using System.Text.RegularExpressions;
using FluentAssertions;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Raytha.Application;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Webhooks;

namespace Raytha.Architecture.Tests;

[TestFixture]
public class ApplicationConventionTests
{
    private static readonly Regex EventNamePattern = new("^[a-z][a-z0-9_]*\\.[a-z][a-z0-9_]*$");

    [Test]
    public void Every_background_task_in_application_is_registered_for_dependency_injection()
    {
        // QueuedHostedService resolves tasks by their assembly-qualified name, so a task that
        // is not registered fails only at runtime when it is first dequeued.
        var services = new ServiceCollection();
        services.AddApplicationServices();
        var registered = services.Select(d => d.ServiceType).ToHashSet();

        var taskTypes = Layers
            .Application.SafeGetTypes()
            .Where(t =>
                t is { IsClass: true, IsAbstract: false }
                && typeof(IExecuteBackgroundTask).IsAssignableFrom(t)
            )
            .ToList();

        taskTypes.Should().NotBeEmpty();
        taskTypes
            .Where(t => !registered.Contains(t))
            .Should()
            .BeEmpty("all IExecuteBackgroundTask implementations must be registered in AddApplicationServices");
    }

    [Test]
    public void Webhook_event_names_are_unique_and_dot_namespaced()
    {
        var attributes = Layers
            .Application.SafeGetTypes()
            .Select(t => t.GetCustomAttributes(typeof(WebhookEventAttribute), false).SingleOrDefault())
            .OfType<WebhookEventAttribute>()
            .ToList();

        attributes.Should().NotBeEmpty("the starter webhook catalog should annotate commands");
        attributes.Select(a => a.EventName).Should().OnlyHaveUniqueItems();
        attributes
            .Select(a => a.EventName)
            .Should()
            .OnlyContain(name => EventNamePattern.IsMatch(name), "event names look like resource.action");
    }

    [Test]
    public void Webhook_annotated_types_declare_a_nested_mediator_command()
    {
        var annotated = Layers
            .Application.SafeGetTypes()
            .Where(t => t.GetCustomAttributes(typeof(WebhookEventAttribute), false).Any())
            .ToList();

        foreach (var type in annotated)
        {
            var command = type.GetNestedType("Command");
            command
                .Should()
                .NotBeNull($"{type.Name} carries [WebhookEvent] but has no nested Command record");
            command!
                .GetInterfaces()
                .Should()
                .Contain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));
        }
    }

    [Test]
    public void Webhook_event_catalog_discovers_every_annotated_command()
    {
        var catalog = new WebhookEventCatalog();
        var annotatedCount = Layers
            .Application.SafeGetTypes()
            .Count(t => t.GetCustomAttributes(typeof(WebhookEventAttribute), false).Any());

        catalog.Events.Should().HaveCount(annotatedCount);
        catalog.IsKnownEvent("*").Should().BeTrue();
        catalog.IsKnownEvent("content_item.created").Should().BeTrue();
        catalog.IsKnownEvent("does.not_exist").Should().BeFalse();
    }

    /// <summary>
    /// Pre-2.0 commands that deliberately bypass the audit log (UI-state tweaks with no
    /// business meaning). Add here only with a reason; new commands must be auditable.
    /// </summary>
    private static readonly HashSet<string> AuditExemptCommands = new(StringComparer.Ordinal)
    {
        // Per-admin view presentation state (column/sort layout, favourites).
        "EditColumn",
        "ReorderColumn",
        "EditSort",
        "ReorderSort",
        "ToggleViewAsFavoriteForAdmin",
        "UpdateRecentlyAccessedView",
        // Internal/self-describing operations.
        "SetAsActiveThemeInternal",
        "ExecuteRaythaFunction", // functions are audited by their own trigger records
        "LoginWithApiKey", // every API request would otherwise write an audit row
    };

    [Test]
    public void Commands_that_mutate_state_are_auditable()
    {
        // Every nested Command record under a *.Commands namespace goes through AuditBehavior.
        var commands = Layers
            .Application.SafeGetTypes()
            .Where(t =>
                t.Name == "Command"
                && t.DeclaringType is not null
                && (t.Namespace ?? string.Empty).EndsWith(".Commands", StringComparison.Ordinal)
            )
            .ToList();

        commands.Should().NotBeEmpty();
        commands
            .Where(t => !typeof(ILoggableRequest).IsAssignableFrom(t))
            .Select(t => t.DeclaringType!.Name)
            .Where(name => !AuditExemptCommands.Contains(name))
            .Should()
            .BeEmpty("commands must derive from LoggableRequest/LoggableEntityRequest so they are audited");
    }

    [Test]
    public void Pipeline_registers_webhook_publish_behavior()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();

        services
            .Where(d => d.ServiceType == typeof(IPipelineBehavior<,>))
            .Select(d => d.ImplementationType)
            .Should()
            .Contain(typeof(Raytha.Application.Common.Behaviors.WebhookPublishBehavior<,>));
    }
}
