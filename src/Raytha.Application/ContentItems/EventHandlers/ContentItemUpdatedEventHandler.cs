using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Shared;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Events;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.ContentItems.EventHandlers;

public class ContentItemUpdatedEventHandler : INotificationHandler<ContentItemUpdatedEvent>
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IRaythaDbContext _db;

    public ContentItemUpdatedEventHandler(
        IBackgroundTaskQueue taskQueue,
        IRaythaDbContext db)
    {
        _taskQueue = taskQueue;
        _db = db;
    }

    public async Task Handle(ContentItemUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var activeFunctions = _db.RaythaFunctions.Where(p => p.IsActive && p.TriggerType == RaythaFunctionTriggerType.ContentItemUpdated.DeveloperName);
        if (activeFunctions.Any())
        {
            var activeThemeId = await _db.OrganizationSettings
                .Select(os => os.ActiveThemeId)
                .FirstAsync(cancellationToken);

            var webTemplate = await _db.WebTemplateContentItemRelations
                .Where(wtr => wtr.ContentItemId == notification.ContentItem.Id && wtr.WebTemplate!.ThemeId == activeThemeId)
                .Include(wtr => wtr.WebTemplate)
                    .ThenInclude(wt => wt.TemplateAccessToModelDefinitions)
                        .ThenInclude(md => md.ContentType)
                .IncludeParentTemplates(wtr => wtr.WebTemplate!.ParentTemplate)
                .Select(wtr => wtr.WebTemplate)
                .FirstAsync(cancellationToken);

            foreach (var activeFunction in activeFunctions)
            {
                await _taskQueue.EnqueueAsync<RaythaFunctionAsBackgroundTask>(new RaythaFunctionAsBackgroundTaskPayload 
                {
                    Target = ContentItemRaythaFunctionTargetDto.GetProjection(notification.ContentItem, webTemplate),
                    RaythaFunction = activeFunction
                }, cancellationToken);
            }
        }
    }
}
