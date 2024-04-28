using MediatR;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Shared;
using Raytha.Domain.Events;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.ContentItems.EventHandlers;

public class ContentItemDeletedEventHandler : INotificationHandler<ContentItemDeletedEvent>
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IRaythaDbContext _db;

    public ContentItemDeletedEventHandler(
        IBackgroundTaskQueue taskQueue,
        IRaythaDbContext db)
    {
        _taskQueue = taskQueue;
        _db = db;
    }

    public async Task Handle(ContentItemDeletedEvent notification, CancellationToken cancellationToken)
    {
        var activeFunctions = _db.RaythaFunctions.Where(p => p.IsActive && p.TriggerType == RaythaFunctionTriggerType.ContentItemDeleted.DeveloperName);
        if (activeFunctions.Any())
        {
            foreach (var activeFunction in activeFunctions)
            {
                await _taskQueue.EnqueueAsync<RaythaFunctionAsBackgroundTask>(new RaythaFunctionAsBackgroundTaskPayload 
                {
                    Target = ContentItemDto.GetProjection(notification.ContentItem),
                    RaythaFunction = activeFunction
                }, cancellationToken);
            }
        }
    }
}
