using CSharpVitamins;
using MediatR;

namespace Raytha.Application.Common.Models;

public interface ILoggableRequest
{
}

public abstract record LoggableRequest<T> : IRequest<T>, ILoggableRequest
{
    public virtual string GetLogName()
    {
        return this.GetType()
            .FullName
            .Replace("Raytha.Application.", string.Empty)
            .Replace("+Command", string.Empty);
    }
}

public interface ILoggableEntityRequest
{
}

public abstract record LoggableEntityRequest<T> : LoggableRequest<T>, ILoggableEntityRequest
{
    public virtual ShortGuid Id { get; init; }
}