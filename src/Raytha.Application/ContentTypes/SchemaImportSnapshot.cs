using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using static Raytha.Application.ContentTypes.SchemaImportPlanner;

namespace Raytha.Application.ContentTypes;

/// <summary>Reads the site as <see cref="SchemaImportPlanner"/> needs to see it.</summary>
public static class SchemaImportSnapshot
{
    public static ExistingSchema Load(IRaythaDbContext db)
    {
        var contentTypes = db
            .ContentTypes.Include(ct => ct.ContentTypeFields)
            .Include(ct => ct.Views)
            .ThenInclude(v => v.Route)
            .ToList();

        var deletedFieldNames = db
            .ContentTypeFields.IgnoreQueryFilters()
            .Where(f => f.IsDeleted)
            .Select(f => new { f.ContentTypeId, f.DeveloperName })
            .ToList()
            .GroupBy(f => f.ContentTypeId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlySet<string>)g.Select(f => f.DeveloperName!).ToHashSet()
            );

        var routePaths = db
            .Routes.Select(r => r.Path)
            .ToList()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var activeThemeId = db.OrganizationSettings.Select(os => os.ActiveThemeId).First();

        var viewRelations = db
            .WebTemplateViewRelations.Where(r => r.WebTemplate!.ThemeId == activeThemeId)
            .ToList()
            .GroupBy(r => r.ViewId)
            .ToDictionary(g => g.Key, g => g.First());

        var templates = db
            .WebTemplates.Where(t => t.ThemeId == activeThemeId)
            .Select(t => new
            {
                t.Id,
                t.DeveloperName,
                t.IsBaseLayout,
                t.AllowAccessForNewContentTypes,
                Access = t.TemplateAccessToModelDefinitions.Select(a => a.ContentTypeId),
            })
            .ToList()
            .Select(t => new TemplateInfo(
                t.Id,
                t.DeveloperName!,
                t.IsBaseLayout,
                t.AllowAccessForNewContentTypes,
                t.Access.ToHashSet()
            ))
            .ToList();

        return new ExistingSchema(
            contentTypes,
            deletedFieldNames,
            routePaths,
            viewRelations,
            templates
        );
    }
}
