using Raytha.Application.Common.Interfaces;
using Raytha.Application.SitePages;

namespace Raytha.Application.Common.Models.RenderModels;

/// <summary>
/// Render model for Site Pages in Liquid templates.
/// </summary>
public record SitePage_RenderModel : IInsertTemplateVariable
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string RoutePath { get; init; } = string.Empty;
    public bool IsPublished { get; init; }
    public bool IsDraft { get; init; }
    public string WebTemplateId { get; init; } = string.Empty;
    public string WebTemplateDeveloperName { get; init; } = string.Empty;
    public DateTime CreationTime { get; init; }
    public DateTime? LastModificationTime { get; init; }

    public static SitePage_RenderModel GetProjection(
        SitePageDto entity,
        string webTemplateDeveloperName
    )
    {
        if (entity == null)
            return null!;

        return new SitePage_RenderModel
        {
            Id = entity.Id,
            Title = entity.Title,
            RoutePath = entity.RoutePath,
            IsPublished = entity.IsPublished,
            IsDraft = entity.IsDraft,
            WebTemplateId = entity.WebTemplateId,
            WebTemplateDeveloperName = webTemplateDeveloperName,
            CreationTime = entity.CreationTime,
            LastModificationTime = entity.LastModificationTime,
        };
    }

    public virtual IEnumerable<string> GetDeveloperNames()
    {
        yield return nameof(Id);
        yield return nameof(Title);
        yield return nameof(RoutePath);
        yield return nameof(IsPublished);
        yield return nameof(IsDraft);
        yield return nameof(WebTemplateId);
        yield return nameof(WebTemplateDeveloperName);
        yield return nameof(CreationTime);
        yield return nameof(LastModificationTime);
    }

    public virtual IEnumerable<KeyValuePair<string, string>> GetTemplateVariables()
    {
        var prefix = "Target";
        foreach (var developerName in GetDeveloperNames())
        {
            yield return new KeyValuePair<string, string>(
                developerName,
                $"{prefix}.{developerName}"
            );
        }
    }

    public virtual string GetTemplateVariablesAsForEachLiquidSyntax()
    {
        return string.Empty;
    }
}

