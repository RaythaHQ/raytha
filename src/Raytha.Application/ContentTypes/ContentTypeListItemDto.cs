using System.Linq.Expressions;
using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.ContentTypes;

public record ContentTypeListItemDto : BaseEntityDto
{
    public bool IsActive { get; init; }
    public string LabelPlural { get; init; } = string.Empty;
    public string LabelSingular { get; init; } = string.Empty;
    public string DeveloperName { get; init; } = string.Empty;
    public string DefaultRouteTemplate { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ShortGuid PrimaryFieldId { get; init; }

    public static Expression<Func<ContentType, ContentTypeListItemDto>> GetProjection()
    {
        return model => GetProjection(model);
    }

    public static ContentTypeListItemDto GetProjection(ContentType entity)
    {
        if (entity == null)
            return null;

        return new ContentTypeListItemDto
        {
            Id = entity.Id,
            LabelPlural = entity.LabelPlural,
            LabelSingular = entity.LabelSingular,
            DeveloperName = entity.DeveloperName,
            IsActive = entity.IsActive,
            Description = entity.Description,
            PrimaryFieldId = entity.PrimaryFieldId,
            DefaultRouteTemplate = entity.DefaultRouteTemplate,
        };
    }
}
