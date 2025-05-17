using System.Text.Json;
using CSharpVitamins;
using MediatR;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;

namespace Raytha.Application.Common.Behaviors;

public class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IRaythaDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AuditBehavior(IRaythaDbContext db, ICurrentUser currentUser)
        : base()
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var response = await next();

        var interfaces = request.GetType().GetInterfaces();

        bool isLoggableRequest = interfaces.Any(p => p == typeof(ILoggableRequest));
        bool isLoggableEntityRequest = interfaces.Any(p => p == typeof(ILoggableEntityRequest));

        if (isLoggableRequest || isLoggableEntityRequest)
        {
            dynamic responseAsDynamic = response as dynamic;
            dynamic requestAsDynamic = request as dynamic;
            if (responseAsDynamic.Success)
            {
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    Request = JsonSerializer.Serialize(requestAsDynamic),
                    Category = requestAsDynamic.GetLogName(),
                    UserEmail = _currentUser.EmailAddress,
                    IpAddress = _currentUser.RemoteIpAddress,
                    EntityId = isLoggableEntityRequest ? (ShortGuid)requestAsDynamic.Id : null,
                };
                _db.AuditLogs.Add(auditLog);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
        return response;
    }
}
