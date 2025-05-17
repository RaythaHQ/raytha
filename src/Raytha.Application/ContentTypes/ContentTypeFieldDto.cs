using System.Linq.Expressions;
using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;

namespace Raytha.Application.ContentTypes;

public record ContentTypeFieldDto : BaseEntityDto
{
    public string DeveloperName { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsRequired { get; init; }
    public string Model { get; init; } = string.Empty;
    public int FieldOrder { get; init; }
    public BaseFieldType? FieldType { get; init; }
    public IEnumerable<ContentTypeFieldChoice> Choices { get; init; } =
        new List<ContentTypeFieldChoice>();
    public ShortGuid? RelatedContentTypeId { get; init; }

    public static Expression<Func<ContentTypeField, ContentTypeFieldDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static ContentTypeFieldDto GetProjection(ContentTypeField entity)
    {
        return new ContentTypeFieldDto
        {
            Id = entity.Id,
            Label = entity.Label,
            Description = entity.Description,
            FieldType = entity.FieldType,
            Model = entity.ContentType.LabelPlural,
            DeveloperName = entity.DeveloperName,
            Choices = entity.Choices,
            IsRequired = entity.IsRequired,
            FieldOrder = entity.FieldOrder,
            RelatedContentTypeId = entity.RelatedContentTypeId,
        };
    }
}
