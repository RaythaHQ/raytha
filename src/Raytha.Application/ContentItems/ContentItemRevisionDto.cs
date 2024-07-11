using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;
using System.Linq.Expressions;

namespace Raytha.Application.ContentItems;

public record ContentItemRevisionDto
{
    public ShortGuid Id { get; init; }
    public AuditableUserDto? CreatorUser { get; init; }
    public AuditableUserDto? LastModifierUser { get; init; }
    public DateTime CreationTime { get; init; }
    public DateTime? LastModificationTime { get; init; }
    public dynamic PublishedContent { get; init; }

    public static Expression<Func<ContentItemRevision, ContentItemRevisionDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }
    public static ContentItemRevisionDto GetProjection(ContentItemRevision entity)
    {
        return new ContentItemRevisionDto
        {
            Id = entity.Id,
            CreatorUser = AuditableUserDto.GetProjection(entity.CreatorUser),
            CreationTime = entity.CreationTime,
            LastModifierUser = AuditableUserDto.GetProjection(entity.LastModifierUser),
            LastModificationTime = entity.LastModificationTime,
            PublishedContent = entity.PublishedContent,
        };
    }
}
