using System.Linq.Expressions;
using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.Themes.WebTemplates;

public record WebTemplateRevisionDto : BaseAuditableEntityDto
{
    public string Content { get; init; } = string.Empty;
    public ShortGuid WebTemplateId { get; init; }
    public string Label { get; init; } = string.Empty;
    public AuditableUserDto? CreatorUser { get; init; }
    public WebTemplateDto? WebTemplate { get; init; }
    public bool AllowAccessForNewContentTypes { get; init; }

    public static Expression<Func<WebTemplateRevision, WebTemplateRevisionDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static WebTemplateRevisionDto GetProjection(WebTemplateRevision entity)
    {
        if (entity == null)
            return null;

        return new WebTemplateRevisionDto
        {
            Id = entity.Id,
            WebTemplateId = entity.WebTemplateId,
            Label = entity.Label,
            Content = entity.Content,
            CreatorUserId = entity.CreatorUserId,
            CreationTime = entity.CreationTime,
            CreatorUser = AuditableUserDto.GetProjection(entity.CreatorUser),
            WebTemplate = WebTemplateDto.GetProjection(entity.WebTemplate),
            AllowAccessForNewContentTypes = entity.AllowAccessForNewContentTypes
        };
    }
}