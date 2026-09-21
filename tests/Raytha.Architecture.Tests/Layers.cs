using System.Reflection;

namespace Raytha.Architecture.Tests;

/// <summary>Assembly handles for the four Raytha layers, resolved via a well-known type in each.</summary>
internal static class Layers
{
    public static readonly Assembly Domain = typeof(Raytha.Domain.Common.BaseEntity).Assembly;
    public static readonly Assembly Application = typeof(Raytha.Application.ConfigureServices).Assembly;
    public static readonly Assembly Infrastructure =
        typeof(Raytha.Infrastructure.Persistence.RaythaDbContext).Assembly;
    public static readonly Assembly Web = typeof(Raytha.Web.Startup).Assembly;

    public static IEnumerable<string> ReferencedAssemblyNames(this Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty);

    public static bool References(this Assembly assembly, Assembly other) =>
        assembly.ReferencedAssemblyNames().Contains(other.GetName().Name);

    public static bool ReferencesAnyStartingWith(this Assembly assembly, string prefix) =>
        assembly
            .ReferencedAssemblyNames()
            .Any(n => n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    public static IEnumerable<Type> SafeGetTypes(this Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null).Cast<Type>();
        }
    }
}
