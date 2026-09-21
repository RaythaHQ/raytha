using Mediator;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Themes.Commands;
using Raytha.Application.Themes.MediaItems.Queries;
using Raytha.Application.Themes.Queries;
using Raytha.Application.Themes.WebTemplates.Commands;
using Raytha.Application.Themes.WebTemplates.Queries;
using Raytha.Application.Themes.WidgetTemplates.Commands;
using Raytha.Application.Themes.WidgetTemplates.Queries;
using Raytha.Domain.Entities;

namespace Raytha.Web.Areas.Admin.Api;

/// <summary>
/// Themes and the web templates that belong to them. Theme export stays on the existing
/// server route <c>/raytha/themes/export/{developerName}</c>.
/// </summary>
public static class ThemesEndpoints
{
    public static RouteGroupBuilder MapThemes(this RouteGroupBuilder admin)
    {
        var themes = admin.MapGroup("/themes")
            .WithTags("Admin themes")
            .RequireAuthorization(BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION);

        themes.MapGet("", List);
        themes.MapGet("/{id}", Get);
        themes.MapPost("", Create);
        themes.MapPut("/{id}", Edit);
        themes.MapDelete("/{id}", Delete);
        themes.MapPost("/{id}/set-active", SetActive);
        themes.MapPut("/{id}/exportability", ToggleExportability);
        themes.MapPost("/{id}/duplicate", Duplicate);
        themes.MapPost("/import", Import);
        themes.MapGet("/{id}/media", ThemeMedia);
        themes.MapGet("/{id}/web-templates/developer-names", WebTemplateDeveloperNames);
        themes.MapGet("/{id}/web-templates/unmatched", UnmatchedWebTemplates);
        themes.MapPost("/{id}/web-templates/match", MatchWebTemplates);

        var templates = themes.MapGroup("/{themeId}/web-templates");
        templates.MapGet("", ListWebTemplates);
        templates.MapGet("/{id}", GetWebTemplate);
        templates.MapGet("/{id}/revisions", WebTemplateRevisions);
        templates.MapPost("", CreateWebTemplateHandler);
        templates.MapPut("/{id}", EditWebTemplateHandler);
        templates.MapDelete("/{id}", DeleteWebTemplateHandler);
        templates.MapPost("/revisions/{revisionId}/revert", RevertWebTemplateHandler);

        var widgets = themes.MapGroup("/{themeId}/widget-templates");
        widgets.MapGet("", ListWidgetTemplates);
        widgets.MapGet("/{id}", GetWidgetTemplate);
        widgets.MapGet("/{id}/revisions", WidgetTemplateRevisions);
        widgets.MapPut("/{id}", EditWidgetTemplateHandler);
        widgets.MapPost("/revisions/{revisionId}/revert", RevertWidgetTemplateHandler);
        widgets.MapPost("/reset", ResetWidgetTemplatesHandler);

        return admin;
    }

    private static async Task<IResult> List([AsParameters] PagedQuery paging, ISender mediator)
    {
        var query = new GetThemes.Query
        {
            PageNumber = paging.PageNumber,
            PageSize = paging.PageSize,
            Search = paging.Search,
        };
        if (paging.HasOrderBy)
        {
            query = query with { OrderBy = paging.OrderBy! };
        }
        return AdminResults.Paged(await mediator.Send(query), paging);
    }

    private static async Task<IResult> Get(string id, ISender mediator) =>
        AdminResults.From(await mediator.Send(new GetThemeById.Query { Id = id }));

    private static async Task<IResult> Create([FromBody] CreateTheme.Command body, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(body), created: true);

    private static async Task<IResult> Edit(string id, [FromBody] EditTheme.Command body, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(body with { Id = id }));

    private static async Task<IResult> Delete(string id, ISender mediator) =>
        AdminResults.NoContent(await mediator.Send(new DeleteTheme.Command { Id = id }));

    private static async Task<IResult> SetActive(string id, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(new SetAsActiveTheme.Command { Id = id }));

    private static async Task<IResult> ToggleExportability(
        string id,
        [FromBody] ToggleThemeExportability.Command body,
        ISender mediator
    ) => AdminResults.FromId(await mediator.Send(body with { Id = id }));

    public sealed record DuplicateThemeRequest(string Title, string DeveloperName, string Description);

    /// <summary>Returns the background task id; poll <c>/background-tasks/{id}</c> for progress.</summary>
    private static async Task<IResult> Duplicate(
        string id,
        [FromBody] DuplicateThemeRequest body,
        ISender mediator,
        ICurrentOrganization org
    ) =>
        AdminResults.FromId(
            await mediator.Send(
                new BeginDuplicateTheme.Command
                {
                    ThemeId = id,
                    Title = body.Title,
                    DeveloperName = body.DeveloperName,
                    Description = body.Description,
                    PathBase = org.PathBase,
                }
            )
        );

    /// <summary>Returns the background task id; poll <c>/background-tasks/{id}</c> for progress.</summary>
    private static async Task<IResult> Import([FromBody] BeginImportThemeFromUrl.Command body, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(body));

    private static async Task<IResult> ThemeMedia(string id, ISender mediator, IRelativeUrlBuilder urls)
    {
        var response = await mediator.Send(new GetMediaItemsByThemeId.Query { ThemeId = id });
        return AdminResults.From(
            response,
            items =>
                items.Select(m => new
                {
                    id = m.Id.ToString(),
                    fileName = m.FileName,
                    contentType = m.ContentType,
                    length = m.Length,
                    objectKey = m.ObjectKey,
                    url = urls.MediaRedirectToFileUrl(m.ObjectKey),
                })
        );
    }

    private static async Task<IResult> WebTemplateDeveloperNames(string id, ISender mediator) =>
        AdminResults.From(await mediator.Send(new GetWebTemplateDeveloperNamesByThemeId.Query { ThemeId = id }));

    private static async Task<IResult> UnmatchedWebTemplates(string id, ISender mediator) =>
        AdminResults.From(await mediator.Send(new GetWebTemplateDeveloperNamesWithoutRelation.Query { ThemeId = id }));

    private static async Task<IResult> MatchWebTemplates(
        string id,
        [FromBody] BeginMatchWebTemplates.Command body,
        ISender mediator
    ) => AdminResults.FromId(await mediator.Send(body with { Id = id }));

    private static async Task<IResult> ListWebTemplates(
        string themeId,
        [AsParameters] PagedQuery paging,
        [FromQuery] bool? baseLayoutsOnly,
        [FromQuery] string? contentTypeId,
        ISender mediator
    )
    {
        var query = new GetWebTemplates.Query
        {
            ThemeId = themeId,
            BaseLayoutsOnly = baseLayoutsOnly ?? false,
            ContentTypeId = string.IsNullOrWhiteSpace(contentTypeId) ? null : contentTypeId,
            PageNumber = paging.PageNumber,
            PageSize = paging.PageSize,
            Search = paging.Search,
        };
        if (paging.HasOrderBy)
        {
            query = query with { OrderBy = paging.OrderBy! };
        }
        return AdminResults.Paged(await mediator.Send(query), paging);
    }

    private static async Task<IResult> GetWebTemplate(string themeId, string id, ISender mediator) =>
        AdminResults.From(await mediator.Send(new GetWebTemplateById.Query { Id = id }));

    private static async Task<IResult> WebTemplateRevisions(
        string themeId,
        string id,
        [AsParameters] PagedQuery paging,
        ISender mediator
    )
    {
        var query = new GetWebTemplateRevisionsByTemplateId.Query
        {
            Id = id,
            PageNumber = paging.PageNumber,
            PageSize = paging.PageSize,
        };
        if (paging.HasOrderBy)
        {
            query = query with { OrderBy = paging.OrderBy! };
        }
        return AdminResults.Paged(await mediator.Send(query), paging);
    }

    private static async Task<IResult> CreateWebTemplateHandler(
        string themeId,
        [FromBody] CreateWebTemplate.Command body,
        ISender mediator
    ) => AdminResults.FromId(await mediator.Send(body with { ThemeId = themeId }), created: true);

    private static async Task<IResult> EditWebTemplateHandler(
        string themeId,
        string id,
        [FromBody] EditWebTemplate.Command body,
        ISender mediator
    ) => AdminResults.FromId(await mediator.Send(body with { Id = id }));

    private static async Task<IResult> DeleteWebTemplateHandler(string themeId, string id, ISender mediator) =>
        AdminResults.NoContent(await mediator.Send(new DeleteWebTemplate.Command { Id = id }));

    private static async Task<IResult> RevertWebTemplateHandler(string themeId, string revisionId, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(new RevertWebTemplate.Command { Id = revisionId }));

    private static async Task<IResult> ListWidgetTemplates(
        string themeId,
        [AsParameters] PagedQuery paging,
        ISender mediator
    )
    {
        var query = new GetWidgetTemplates.Query
        {
            ThemeId = themeId,
            PageNumber = paging.PageNumber,
            PageSize = paging.PageSize,
            Search = paging.Search,
        };
        if (paging.HasOrderBy)
        {
            query = query with { OrderBy = paging.OrderBy! };
        }
        return AdminResults.Paged(await mediator.Send(query), paging);
    }

    private static async Task<IResult> GetWidgetTemplate(string themeId, string id, ISender mediator) =>
        AdminResults.From(await mediator.Send(new GetWidgetTemplateById.Query { Id = id }));

    private static async Task<IResult> WidgetTemplateRevisions(
        string themeId,
        string id,
        [AsParameters] PagedQuery paging,
        ISender mediator
    )
    {
        var query = new GetWidgetTemplateRevisionsByTemplateId.Query
        {
            Id = id,
            PageNumber = paging.PageNumber,
            PageSize = paging.PageSize,
        };
        if (paging.HasOrderBy)
        {
            query = query with { OrderBy = paging.OrderBy! };
        }
        return AdminResults.Paged(await mediator.Send(query), paging);
    }

    private static async Task<IResult> EditWidgetTemplateHandler(
        string themeId,
        string id,
        [FromBody] EditWidgetTemplate.Command body,
        ISender mediator
    ) => AdminResults.FromId(await mediator.Send(body with { Id = id }));

    private static async Task<IResult> RevertWidgetTemplateHandler(string themeId, string revisionId, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(new RevertWidgetTemplate.Command { Id = revisionId }));

    private static async Task<IResult> ResetWidgetTemplatesHandler(string themeId, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(new ResetWidgetTemplatesToDefault.Command { ThemeId = themeId }));
}
