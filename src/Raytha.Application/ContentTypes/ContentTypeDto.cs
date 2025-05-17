using System.Linq.Expressions;
using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.ContentTypes;

public record ContentTypeDto : BaseEntityDto
{
    public bool IsActive { get; init; }
    public string LabelPlural { get; init; } = string.Empty;
    public string LabelSingular { get; init; } = string.Empty;
    public string DeveloperName { get; init; } = string.Empty;
    public string DefaultRouteTemplate { get; init; } = string.Empty;
    public IEnumerable<ContentTypeFieldDto> ContentTypeFields { get; init; } =
        new List<ContentTypeFieldDto>();
    public string Description { get; init; } = string.Empty;
    public ShortGuid PrimaryFieldId { get; init; }

    public ContentTypeFieldDto PrimaryField
    {
        get { return ContentTypeFields.FirstOrDefault(p => p.Id == PrimaryFieldId); }
    }

    public ContentTypeFieldDto GetCustomField(string developerName)
    {
        return ContentTypeFields.FirstOrDefault(p => p.DeveloperName == developerName);
    }

    public static Expression<Func<ContentType, ContentTypeDto>> GetProjection()
    {
        return model => GetProjection(model);
    }

    public static ContentTypeDto GetProjection(ContentType entity)
    {
        if (entity == null)
            return null;

        return new ContentTypeDto
        {
            Id = entity.Id,
            LabelPlural = entity.LabelPlural,
            LabelSingular = entity.LabelSingular,
            DeveloperName = entity.DeveloperName,
            IsActive = entity.IsActive,
            ContentTypeFields = entity
                .ContentTypeFields?.AsQueryable()
                .OrderBy(p => p.FieldOrder)
                .Select(ContentTypeFieldDto.GetProjection()),
            Description = entity.Description,
            PrimaryFieldId = entity.PrimaryFieldId,
            DefaultRouteTemplate = entity.DefaultRouteTemplate,
        };
    }
}
