using System.Linq.Expressions;
using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Application.Themes.WebTemplates;
using Raytha.Domain.Entities;

namespace Raytha.Application.SitePages;

public record SitePageDto : BaseAuditableEntityDto
{
    /// <summary>
    /// The page title.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Whether the page is published and visible to the public.
    /// </summary>
    public bool IsPublished { get; init; }

    /// <summary>
    /// Whether the page has unpublished draft changes.
    /// </summary>
    public bool IsDraft { get; init; }

    /// <summary>
    /// The route ID for URL path.
    /// </summary>
    public ShortGuid RouteId { get; init; }

    /// <summary>
    /// The URL path for this page.
    /// </summary>
    public string RoutePath { get; init; } = string.Empty;

    /// <summary>
    /// The web template ID used to render this page.
    /// </summary>
    public ShortGuid WebTemplateId { get; init; }

    /// <summary>
    /// The web template used to render this page.
    /// </summary>
    public WebTemplateDto? WebTemplate { get; init; }

    /// <summary>
    /// Creator user information.
    /// </summary>
    public AuditableUserDto? CreatorUser { get; init; }

    /// <summary>
    /// Last modifier user information.
    /// </summary>
    public AuditableUserDto? LastModifierUser { get; init; }

    /// <summary>
    /// Dictionary of widgets organized by section name.
    /// Key = section developer name (e.g., "hero", "main", "sidebar")
    /// Value = list of widgets for that section
    /// </summary>
    public Dictionary<string, List<SitePageWidgetDto>> Widgets { get; init; } =
        new Dictionary<string, List<SitePageWidgetDto>>();

    public static Expression<Func<SitePage, SitePageDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static SitePageDto GetProjection(SitePage entity)
    {
        if (entity == null)
            return null!;

        return new SitePageDto
        {
            Id = entity.Id,
            Title = entity.Title,
            IsPublished = entity.IsPublished,
            IsDraft = entity.IsDraft,
            RouteId = entity.RouteId,
            RoutePath = entity.Route?.Path ?? string.Empty,
            WebTemplateId = entity.WebTemplateId,
            WebTemplate = WebTemplateDto.GetProjection(entity.WebTemplate),
            CreatorUserId = entity.CreatorUserId,
            CreationTime = entity.CreationTime,
            LastModificationTime = entity.LastModificationTime,
            LastModifierUserId = entity.LastModifierUserId,
            CreatorUser = AuditableUserDto.GetProjection(entity.CreatorUser),
            LastModifierUser = AuditableUserDto.GetProjection(entity.LastModifierUser),
            Widgets = entity.Widgets.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Select(SitePageWidgetDto.GetProjection).ToList()
            ),
        };
    }

    public override string ToString()
    {
        return Title;
    }
}
