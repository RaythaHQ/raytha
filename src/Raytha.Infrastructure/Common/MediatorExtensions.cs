using Raytha.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace MediatR;

public static class MediatorExtensions
{
    public static async Task DispatchDomainEventsBeforeSaveChanges(this IMediator mediator, DbContext context)
    {
        var entities = context.ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any(p => p is IBeforeSaveChangesNotification))
            .Select(e => e.Entity);

        var domainEvents = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        entities.ToList().ForEach(e => e.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
            await mediator.Publish(domainEvent);
    }

    public static async Task DispatchDomainEventsAfterSaveChanges(this IMediator mediator, DbContext context)
    {
        var entities = context.ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any(p => p is IAfterSaveChangesNotification || (p is not IAfterSaveChangesNotification && p is not IBeforeSaveChangesNotification)))
            .Select(e => e.Entity);

        var domainEvents = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        entities.ToList().ForEach(e => e.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
            await mediator.Publish(domainEvent);
    }
}