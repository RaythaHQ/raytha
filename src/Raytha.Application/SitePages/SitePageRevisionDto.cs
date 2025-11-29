using System.Linq.Expressions;
using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.SitePages;

public record SitePageRevisionDto
{
    public ShortGuid Id { get; init; }
    public AuditableUserDto? CreatorUser { get; init; }
    public AuditableUserDto? LastModifierUser { get; init; }
    public DateTime CreationTime { get; init; }
    public DateTime? LastModificationTime { get; init; }

    /// <summary>
    /// Dictionary of published widgets organized by section name at this revision.
    /// </summary>
    public Dictionary<string, List<SitePageWidgetDto>> PublishedWidgets { get; init; } =
        new Dictionary<string, List<SitePageWidgetDto>>();

    public static Expression<Func<SitePageRevision, SitePageRevisionDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static SitePageRevisionDto GetProjection(SitePageRevision entity)
    {
        return new SitePageRevisionDto
        {
            Id = entity.Id,
            CreatorUser = AuditableUserDto.GetProjection(entity.CreatorUser),
            CreationTime = entity.CreationTime,
            LastModifierUser = AuditableUserDto.GetProjection(entity.LastModifierUser),
            LastModificationTime = entity.LastModificationTime,
            PublishedWidgets = entity.PublishedWidgets.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Select(SitePageWidgetDto.GetProjection).ToList()
            ),
        };
    }
}

