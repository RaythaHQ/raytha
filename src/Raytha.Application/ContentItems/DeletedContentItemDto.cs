using System.Linq.Expressions;
using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.ContentItems;

public record DeletedContentItemDto
{
    public ShortGuid Id { get; init; }
    public ShortGuid OriginalContentItemId { get; init; }
    public string PrimaryField { get; set; }
    public AuditableUserDto? CreatorUser { get; init; }
    public AuditableUserDto? LastModifierUser { get; init; }
    public DateTime CreationTime { get; init; }
    public DateTime? LastModificationTime { get; init; }

    public static Expression<Func<DeletedContentItem, DeletedContentItemDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static DeletedContentItemDto GetProjection(DeletedContentItem entity)
    {
        return new DeletedContentItemDto
        {
            Id = entity.Id,
            CreatorUser = AuditableUserDto.GetProjection(entity.CreatorUser),
            CreationTime = entity.CreationTime,
            LastModifierUser = AuditableUserDto.GetProjection(entity.LastModifierUser),
            LastModificationTime = entity.LastModificationTime,
            PrimaryField = entity.PrimaryField,
            OriginalContentItemId = entity.OriginalContentItemId,
        };
    }

    public override string ToString()
    {
        return PrimaryField;
    }
}
