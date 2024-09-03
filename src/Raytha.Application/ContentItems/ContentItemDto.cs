using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Application.ContentTypes;
using Raytha.Domain.Entities;
using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace Raytha.Application.ContentItems;

public record ContentItemDto : BaseEntityDto
{
    public AuditableUserDto? CreatorUser { get; init; }
    public AuditableUserDto? LastModifierUser { get; init; }
    public DateTime CreationTime { get; init; }
    public DateTime? LastModificationTime { get; init; }
    public bool IsPublished { get; init; }
    public bool IsDraft { get; init; }
    public ShortGuid ContentTypeId { get; init; }

    [JsonIgnore]
    public ContentTypeDto? ContentType { get; init; }

    [JsonIgnore]
    public ShortGuid RouteId { get; init; }
    public string RoutePath { get; init; }
    public string PrimaryField { get; init; }
    public dynamic PublishedContent { get; init; }
    public dynamic DraftContent { get; init; }

    public static Expression<Func<ContentItem, ContentItemDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static ContentItemDto GetProjection(ContentItem entity)
    {
        return new ContentItemDto
        {
            Id = entity.Id,
            CreatorUser = AuditableUserDto.GetProjection(entity.CreatorUser),
            CreationTime = entity.CreationTime,
            LastModifierUser = AuditableUserDto.GetProjection(entity.LastModifierUser),
            LastModificationTime = entity.LastModificationTime,
            IsDraft = entity.IsDraft,
            IsPublished = entity.IsPublished,
            ContentTypeId = entity.ContentTypeId,
            ContentType = ContentTypeDto.GetProjection(entity.ContentType),
            PrimaryField = entity.PrimaryField,
            PublishedContent = entity.PublishedContent,
            DraftContent = entity.DraftContent,
            RouteId = entity.RouteId,
            RoutePath = entity.Route.Path
        };
    }

    public override string ToString()
    {
        return PrimaryField;
    }
}