using System.Reflection;
using Raytha.Application.Common.Interfaces;

namespace Raytha.Web.Services;

public class CurrentVersion : ICurrentVersion
{
    public string Version { get; } =
        Assembly
            .GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion?.Split('+')[0]
        ?? "0.0.0";
}
