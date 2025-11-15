namespace Raytha.Domain.Common;

public interface ISoftDelete
{
    bool IsDeleted { get; set; }
}

public interface IPassivable
{
    bool IsActive { get; set; }
}

public interface IDeletionAuditable : ISoftDelete
{
    Guid? DeleterUserId { get; set; }

    DateTime? DeletionTime { get; set; }
}

public interface IFullAuditable : IAuditable, IDeletionAuditable { }

public abstract class BaseFullAuditableEntity : BaseAuditableEntity, IFullAuditable
{
    public Guid? DeleterUserId { get; set; }
    public DateTime? DeletionTime { get; set; }
    public bool IsDeleted { get; set; } = false;
}
