
using System.ComponentModel.DataAnnotations.Schema;

namespace Raytha.Domain.Common;

public interface IBaseEntity
{
    Guid Id { get; set; }
}

public abstract class BaseEntity : IBaseEntity
{
    public Guid Id { get; set; }

    private readonly List<BaseEvent> _domainEvents = new();

    [NotMapped]
    public IReadOnlyCollection<BaseEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(BaseEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void RemoveDomainEvent(BaseEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}