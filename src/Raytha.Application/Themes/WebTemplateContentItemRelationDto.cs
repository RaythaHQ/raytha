using System.Linq.Expressions;
using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Application.Themes.WebTemplates;
using Raytha.Domain.Entities;

namespace Raytha.Application.Themes;

public record WebTemplateContentItemRelationDto : BaseEntityDto
{
    public required WebTemplateDto WebTemplate { get; init; } 
    public required ShortGuid ContentItemId { get; init; }

    public static Expression<Func<WebTemplateContentItemRelation, WebTemplateContentItemRelationDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static WebTemplateContentItemRelationDto GetProjection(WebTemplateContentItemRelation entity)
    {
        return new WebTemplateContentItemRelationDto
        {
            Id = entity.Id,
            WebTemplate = WebTemplateDto.GetProjection(entity.WebTemplate)!,
            ContentItemId = entity.ContentItemId,
        };
    }
}