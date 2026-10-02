using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using FluentAssertions;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Raytha.Architecture.Tests;

/// <summary>
/// RFC-0002: a use case is one outer class holding a nested <c>Command</c> or <c>Query</c>, the
/// <c>Handler</c> for it, and optionally a <c>Validator</c> for it.
/// </summary>
[TestFixture]
public class CqrsStructureTests
{
    private const BindingFlags Declared =
        BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    private static readonly Type[] ApplicationTypes = Layers.Application.SafeGetTypes().ToArray();

    private static readonly Type[] Requests = ApplicationTypes
        .Where(t => t is { IsClass: true, IsAbstract: false } && ClosedOver(t, typeof(IRequest<>)).Any())
        .ToArray();

    private static readonly Type[] Handlers = ApplicationTypes
        .Where(t => t is { IsClass: true, IsAbstract: false } && ClosedOver(t, typeof(IRequestHandler<,>)).Any())
        .ToArray();

    private static readonly Type[] Validators = ApplicationTypes
        .Where(t => t is { IsClass: true, IsAbstract: false } && ValidatedType(t) is not null)
        .ToArray();

    private static IEnumerable<Type> ClosedOver(Type type, Type openInterface) =>
        type.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openInterface);

    private static Type? ValidatedType(Type type)
    {
        for (var t = type.BaseType; t is not null; t = t.BaseType)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(AbstractValidator<>))
                return t.GetGenericArguments()[0];
        }
        return null;
    }

    private static IEnumerable<Type> HandledRequests(Type handler) =>
        ClosedOver(handler, typeof(IRequestHandler<,>)).Select(i => i.GetGenericArguments()[0]);

    private static bool IsQuery(Type request) => request.Name == "Query";

    private static string Describe(IEnumerable<string> violations) => string.Join(Environment.NewLine, violations.Order());

    private static void ShouldOnlyViolateExempted(
        IEnumerable<(string Key, string Message)> violations,
        IReadOnlyDictionary<string, string> exempt,
        string because
    )
    {
        var found = violations.ToList();
        Describe(found.Where(v => !exempt.ContainsKey(v.Key)).Select(v => v.Message)).Should().BeEmpty(because);
        Describe(exempt.Keys.Except(found.Select(v => v.Key)))
            .Should()
            .BeEmpty("an exemption whose violation has been fixed must leave the list");
    }

    /// <summary>Pre-2.0 violations of RFC-0002. Closed list; entries leave when fixed.</summary>
    private static readonly Dictionary<string, string> OuterClassMemberExemptions = new(StringComparer.Ordinal)
    {
        ["GetEmailLogs.AsUtcDate"] = "date helper; move into Handler once the in-flight email log work settles",
    };

    private static readonly Dictionary<string, string> HandlerDependencyExemptions = new(StringComparer.Ordinal)
    {
        ["CreateNavigationMenuItem"] = "sends CreateNavigationMenuRevision; extract the revision snapshot",
        ["EditNavigationMenuItem"] = "sends CreateNavigationMenuRevision; extract the revision snapshot",
        ["DeleteNavigationMenuItem"] = "sends CreateNavigationMenuRevision; extract the revision snapshot",
        ["RevertNavigationMenu"] = "sends CreateNavigationMenuRevision; extract the revision snapshot",
        ["SetAsActiveTheme"] = "sends SetAsActiveThemeInternal; extract the activation logic",
        ["InitialSetup"] = "sends EnsureDefaultThemeContent, which startup also runs; extract the theme seeding",
    };

    private static readonly Dictionary<string, string> ValidatorSaveExemptions = new(StringComparer.Ordinal)
    {
        ["LoginWithEmailAndPassword"] =
            "the validator records and prunes FailedLoginAttempt rows for lockout; move that into the handler",
    };

    [Test]
    public void Discovery_sees_the_use_cases()
    {
        Requests.Should().HaveCountGreaterThan(150);
        Handlers.Should().HaveCountGreaterThan(150);
        Validators.Should().HaveCountGreaterThan(50);
    }

    [Test]
    public void Requests_are_nested_Command_or_Query_types_in_the_matching_namespace()
    {
        var violations = Requests
            .Where(r =>
                r.DeclaringType is not { DeclaringType: null }
                || !(
                    (r.Name == "Command" && (r.Namespace ?? "").EndsWith(".Commands", StringComparison.Ordinal))
                    || (r.Name == "Query" && (r.Namespace ?? "").EndsWith(".Queries", StringComparison.Ordinal))
                )
            )
            .Select(r => r.FullName!);

        Describe(violations)
            .Should()
            .BeEmpty("a request is a nested Command in <Feature>.Commands or a nested Query in <Feature>.Queries");
    }

    [Test]
    public void Every_request_has_exactly_one_handler_nested_beside_it()
    {
        var violations = Requests
            .Select(r => (request: r, handlers: Handlers.Where(h => HandledRequests(h).Contains(r)).ToList()))
            .Where(x =>
                x.handlers.Count != 1
                || x.handlers[0].Name != "Handler"
                || x.handlers[0].DeclaringType != x.request.DeclaringType
            )
            .Select(x =>
                $"{x.request.FullName}: handled by [{string.Join(", ", x.handlers.Select(h => h.FullName))}]"
            );

        Describe(violations).Should().BeEmpty("each Command or Query has one sibling Handler in the same outer class");
    }

    [Test]
    public void Handlers_only_handle_the_request_nested_beside_them()
    {
        var violations = Handlers
            .Where(h => h.Name != "Handler" || HandledRequests(h).Any(r => r.DeclaringType != h.DeclaringType))
            .Select(h => $"{h.FullName} handles [{string.Join(", ", HandledRequests(h).Select(r => r.FullName))}]");

        Describe(violations).Should().BeEmpty("a handler lives in its request's outer class and is named Handler");
    }

    [Test]
    public void Request_validators_validate_the_request_nested_beside_them()
    {
        var useCases = Requests.Select(r => r.DeclaringType).ToHashSet();
        var violations = Validators
            .Where(v =>
            {
                var target = ValidatedType(v)!;
                var validatesRequest = Requests.Contains(target);
                var nestedInUseCase = v.DeclaringType is not null && useCases.Contains(v.DeclaringType);
                return (validatesRequest || nestedInUseCase)
                    && (v.Name != "Validator" || target.DeclaringType != v.DeclaringType);
            })
            .Select(v => $"{v.FullName} validates {ValidatedType(v)!.FullName}");

        Describe(violations).Should().BeEmpty("a use case's Validator validates the Command or Query beside it");
    }

    [Test]
    public void Use_case_outer_classes_hold_no_state_or_logic()
    {
        var violations = Requests
            .Select(r => r.DeclaringType!)
            .Distinct()
            .SelectMany(outer =>
                outer
                    .GetMembers(Declared)
                    .Where(m =>
                        m is FieldInfo or PropertyInfo or EventInfo || (m is MethodInfo method && !method.IsSpecialName)
                    )
                    .Where(m => !m.IsDefined(typeof(CompilerGeneratedAttribute)))
                    .Select(m => ($"{outer.Name}.{m.Name}", $"{outer.FullName}.{m.Name}"))
            );

        ShouldOnlyViolateExempted(
            violations,
            OuterClassMemberExemptions,
            "the outer class is only a namespace for Command/Query, Validator, and Handler"
        );
    }

    [Test]
    public void Commands_are_records_with_init_only_properties()
    {
        var violations = Requests
            .Where(r => !IsQuery(r))
            .SelectMany(command =>
            {
                var problems = new List<string>();
                if (command.GetMethod("<Clone>$") is null)
                    problems.Add($"{command.FullName} is not a record");
                problems.AddRange(
                    command
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.SetMethod is { IsPublic: true } setter && !IsInitOnly(setter))
                        .Select(p => $"{command.FullName}.{p.Name} has a mutable setter")
                );
                return problems;
            });

        Describe(violations).Should().BeEmpty("commands are records with init-only properties");
    }

    private static bool IsInitOnly(MethodInfo setter) =>
        setter.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit));

    [Test]
    public void Query_handlers_never_save_changes()
    {
        var violations = Handlers
            .Where(h => HandledRequests(h).All(IsQuery))
            .Where(h => CalledMethods(h).Any(IsSaveChanges))
            .Select(h => h.FullName!);

        Describe(violations).Should().BeEmpty("a query handler must not call SaveChanges or SaveChangesAsync");
    }

    [Test]
    public void Validators_never_save_changes()
    {
        var violations = Validators
            .Where(v => CalledMethods(v).Any(IsSaveChanges))
            .Select(v => (v.DeclaringType?.Name ?? v.Name, v.FullName!));

        ShouldOnlyViolateExempted(violations, ValidatorSaveExemptions, "validators must not mutate state");
    }

    private static bool IsSaveChanges(MethodBase method) => method.Name is "SaveChanges" or "SaveChangesAsync";

    private static readonly Type[] ForbiddenHandlerDependencies =
    [
        typeof(ISender),
        typeof(IMediator),
        typeof(IServiceProvider),
        typeof(IServiceScopeFactory),
    ];

    [Test]
    public void Handlers_do_not_send_through_the_mediator_or_resolve_services()
    {
        var violations = Handlers.SelectMany(h =>
            h.GetConstructors()
                .SelectMany(c => c.GetParameters())
                .Where(p => ForbiddenHandlerDependencies.Contains(p.ParameterType))
                .Select(p => (h.DeclaringType!.Name, $"{h.FullName} takes {p.ParameterType.Name}"))
        );

        ShouldOnlyViolateExempted(
            violations,
            HandlerDependencyExemptions,
            "handlers must not call another handler through ISender or resolve services from a container"
        );
    }

    private static readonly Dictionary<short, OpCode> OpCodesByValue = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(f => (OpCode)f.GetValue(null)!)
        .ToDictionary(o => o.Value);

    /// <summary>Methods called from a type's bodies, including its async state machines and lambdas.</summary>
    private static IEnumerable<MethodBase> CalledMethods(Type type)
    {
        var bodies = type.GetMethods(Declared).Cast<MethodBase>().Concat(type.GetConstructors(Declared));
        foreach (var body in bodies)
        {
            var il = body.GetMethodBody()?.GetILAsByteArray();
            if (il is null)
                continue;
            for (var i = 0; i < il.Length; )
            {
                var value = il[i] == 0xFE ? (short)(0xFE00 | il[i + 1]) : il[i];
                var opCode = OpCodesByValue[value];
                i += opCode.Size;
                if (opCode.OperandType == OperandType.InlineMethod)
                {
                    MethodBase? called = null;
                    try
                    {
                        called = type.Module.ResolveMethod(BitConverter.ToInt32(il, i));
                    }
                    catch (ArgumentException) { }
                    if (called is not null)
                        yield return called;
                }
                i += opCode.OperandType switch
                {
                    OperandType.InlineNone => 0,
                    OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                    OperandType.InlineVar => 2,
                    OperandType.InlineI8 or OperandType.InlineR => 8,
                    OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, i),
                    _ => 4,
                };
            }
        }
        foreach (var nested in type.GetNestedTypes(BindingFlags.NonPublic).Where(t => t.IsDefined(typeof(CompilerGeneratedAttribute))))
        foreach (var called in CalledMethods(nested))
            yield return called;
    }
}
