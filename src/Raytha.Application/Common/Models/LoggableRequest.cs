using CSharpVitamins;
using Mediator;
using Raytha.Application.Common.Attributes;

namespace Raytha.Application.Common.Models;

public interface ILoggableRequest { }

public abstract record LoggableRequest<T> : IRequest<T>, ILoggableRequest
{
    public virtual string GetLogName()
    {
        return this.GetType()
            .FullName.Replace("Raytha.Application.", string.Empty)
            .Replace("+Command", string.Empty)
            .Replace("NavigationMenu", "Menu")
            .Replace("NavigationMenuItem", "MenuItem")
            .Replace("RaythaFunction", "Function");
    }
}

public interface ILoggableEntityRequest { }

public abstract record LoggableEntityRequest<T> : LoggableRequest<T>, ILoggableEntityRequest
{
    [ExcludePropertyFromOpenApiDocs]
    public virtual ShortGuid Id { get; init; }
}
