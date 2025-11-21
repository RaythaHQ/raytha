using System.Text.Json;
using CSharpVitamins;
using Mediator;
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
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    Request = JsonSerializer.Serialize(messageAsDynamic),
                    Category = messageAsDynamic.GetLogName(),
                    UserEmail = _currentUser.EmailAddress,
                    IpAddress = _currentUser.RemoteIpAddress,
                    EntityId = isLoggableEntityRequest ? (ShortGuid)messageAsDynamic.Id : null,
                };
                _db.AuditLogs.Add(auditLog);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
        return response;
    }
}
