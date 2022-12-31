using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Raytha.Infrastructure.Persistence.Interceptors;

public class AuditableEntitySaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUserService;

    public AuditableEntitySaveChangesInterceptor(
        ICurrentUser currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public void UpdateEntities(DbContext? context)
    {
        if (context == null) return;

        foreach (var entry in context.ChangeTracker.Entries<ICreationAuditable>().Where(p => p.State == EntityState.Added))
        {
            entry.Entity.CreationTime = DateTime.UtcNow;
            entry.Entity.CreatorUserId = _currentUserService?.UserId != Guid.Empty ? _currentUserService?.UserId?.Guid : null;
        }
        foreach (var entry in context.ChangeTracker.Entries<IModificationAuditable>().Where(p => p.State == EntityState.Modified))
        {
            entry.Entity.LastModificationTime = DateTime.UtcNow;
            entry.Entity.LastModifierUserId = _currentUserService?.UserId != Guid.Empty ? _currentUserService?.UserId?.Guid : null;
        }
        foreach (var entry in context.ChangeTracker.Entries<IDeletionAuditable>().Where(p => p.State == EntityState.Deleted))
        {
            entry.State = EntityState.Modified;
            entry.CurrentValues["IsDeleted"] = true;
            entry.Entity.DeleterUserId = _currentUserService?.UserId != Guid.Empty ? _currentUserService?.UserId?.Guid : null;
            entry.Entity.DeletionTime = DateTime.UtcNow;
        }
    }
}