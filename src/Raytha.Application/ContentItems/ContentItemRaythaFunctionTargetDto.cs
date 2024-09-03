using System.Linq.Expressions;
using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Application.ContentTypes;
using Raytha.Application.Themes.WebTemplates;
using Raytha.Domain.Entities;

namespace Raytha.Application.ContentItems;

public record ContentItemRaythaFunctionTargetDto : BaseEntityDto
{
    public bool IsPublished { get; init; }
    public bool IsDraft { get; init; }
    public ShortGuid ContentTypeId { get; init; }
    public ContentTypeDto? ContentType { get; init; }
    public ShortGuid WebTemplateId { get; init; }
    public required WebTemplateDto WebTemplate { get; init; }
    public ShortGuid RouteId { get; init; }
    public string RoutePath { get; init; }
    public string PrimaryField { get; init; }
    public dynamic PublishedContent { get; init; }
    public dynamic DraftContent { get; init; }
    public AuditableUserDto? CreatorUser { get; init; }
    public DateTime CreationTime { get; init; }
    public AuditableUserDto? LastModifierUser { get; init; }
    public DateTime? LastModificationTime { get; init; }

    public static Expression<Func<ContentItem, WebTemplate, ContentItemRaythaFunctionTargetDto>> GetProjection()
    {
        return (contentItem, webTemplate) => GetProjection(contentItem, webTemplate);
    }

    public static ContentItemRaythaFunctionTargetDto GetProjection(ContentItem contentItem, WebTemplate webTemplate)
    {
        return new ContentItemRaythaFunctionTargetDto
        {
            Id = contentItem.Id,
            IsPublished = contentItem.IsPublished,
            IsDraft = contentItem.IsDraft,
            ContentTypeId = contentItem.ContentTypeId,
            ContentType = ContentTypeDto.GetProjection(contentItem.ContentType),
            WebTemplateId = webTemplate.Id,
            WebTemplate = WebTemplateDto.GetProjection(webTemplate)!,
            RouteId = contentItem.RouteId,
            RoutePath = contentItem.Route.Path,
            PrimaryField = contentItem.PrimaryField,
            PublishedContent = contentItem.PublishedContent,
            DraftContent = contentItem.DraftContent,
            CreatorUser = AuditableUserDto.GetProjection(contentItem.CreatorUser),
            CreationTime = contentItem.CreationTime,
            LastModifierUser = AuditableUserDto.GetProjection(contentItem.LastModifierUser),
            LastModificationTime = contentItem.LastModificationTime,
        };
    }

    public override string ToString()
    {
        return PrimaryField;
    }

}