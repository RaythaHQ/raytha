using System.Linq.Expressions;
using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.Themes.WidgetTemplates;

public record WidgetTemplateRevisionDto : BaseAuditableEntityDto
{
    public string Content { get; init; } = string.Empty;
    public ShortGuid WidgetTemplateId { get; init; }
    public string Label { get; init; } = string.Empty;
    public AuditableUserDto? CreatorUser { get; init; }
    public WidgetTemplateDto? WidgetTemplate { get; init; }

    public static Expression<Func<WidgetTemplateRevision, WidgetTemplateRevisionDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static WidgetTemplateRevisionDto GetProjection(WidgetTemplateRevision entity)
    {
        if (entity == null)
            return null;

        return new WidgetTemplateRevisionDto
        {
            Id = entity.Id,
            WidgetTemplateId = entity.WidgetTemplateId,
            Label = entity.Label,
            Content = entity.Content,
            CreatorUserId = entity.CreatorUserId,
            CreationTime = entity.CreationTime,
            CreatorUser = AuditableUserDto.GetProjection(entity.CreatorUser),
            WidgetTemplate = WidgetTemplateDto.GetProjection(entity.WidgetTemplate),
        };
    }
}

