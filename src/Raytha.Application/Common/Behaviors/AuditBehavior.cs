using System.Reflection;
using CSharpVitamins;
using Mediator;
using Raytha.Application.Common.Attributes;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;

namespace Raytha.Application.Common.Behaviors;

public class AuditBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private readonly IRaythaDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AuditBehavior(IRaythaDbContext db, ICurrentUser currentUser)
        : base()
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var response = await next(message, cancellationToken);

        var interfaces = message.GetType().GetInterfaces();

        bool isLoggableRequest = interfaces.Any(p => p == typeof(ILoggableRequest));
        bool isLoggableEntityRequest = interfaces.Any(p => p == typeof(ILoggableEntityRequest));

        if (isLoggableRequest || isLoggableEntityRequest)
        {
            dynamic responseAsDynamic = response as dynamic;
            dynamic messageAsDynamic = message as dynamic;
            if (responseAsDynamic.Success)
            {
                var redactIdentifier = message
                    .GetType()
                    .GetCustomAttribute<AuditCredentialIdentifierAttribute>()
                    is not null;
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    Request = AuditRequestSanitizer.Sanitize(message, redactIdentifier),
                    Category = messageAsDynamic.GetLogName(),
                    UserEmail = _currentUser.EmailAddress,
                    ImpersonatorEmail = _currentUser.ImpersonatorEmailAddress,
                    IpAddress = _currentUser.RemoteIpAddress,
                    EntityId = EntityIdFor(message, response, isLoggableEntityRequest, redactIdentifier),
                };
                _db.AuditLogs.Add(auditLog);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
        return response;
    }

    /// <summary>
    /// A credential identifier (a reset token) must not be stored as the audited
    /// entity. Point the row at the user id the command returned instead.
    /// </summary>
    private static Guid? EntityIdFor(
        object message,
        object response,
        bool isLoggableEntityRequest,
        bool redactIdentifier
    )
    {
        if (!isLoggableEntityRequest)
        {
            return null;
        }

        if (redactIdentifier)
        {
            var result = response.GetType().GetProperty("Result")?.GetValue(response);
            return result is ShortGuid userId && userId.Guid != Guid.Empty ? userId.Guid : null;
        }

        dynamic messageAsDynamic = message;
        ShortGuid id = messageAsDynamic.Id;
        return id.Guid == Guid.Empty ? null : id.Guid;
    }
}
