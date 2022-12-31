namespace Raytha.Domain.Common;


public interface IHasCreationTime
{
    DateTime CreationTime { get; set; }
}

public interface ICreationAuditable : IHasCreationTime
{
    Guid? CreatorUserId { get; set; }
}

public interface IHasModificationTime
{
    DateTime? LastModificationTime { get; set; }
}

public interface IModificationAuditable : IHasModificationTime
{
    Guid? LastModifierUserId { get; set; }
}

public interface IAuditable : ICreationAuditable, IModificationAuditable, IBaseEntity
{
}

public abstract class BaseAuditableEntity : BaseEntity, IAuditable
{
    public DateTime CreationTime { get; set; } = DateTime.UtcNow;
    public DateTime? LastModificationTime { get; set; }

    public Guid? CreatorUserId { get; set; }
    public virtual User? CreatorUser { get; set; }

    public Guid? LastModifierUserId { get; set; }
    public virtual User? LastModifierUser { get; set; }
}