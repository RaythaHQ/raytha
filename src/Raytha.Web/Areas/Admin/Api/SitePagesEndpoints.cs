using Mediator;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.SitePages.Commands;
using Raytha.Application.SitePages.Queries;
using Raytha.Application.SitePages.Widgets;
using Raytha.Domain.Entities;

namespace Raytha.Web.Areas.Admin.Api;

public static class SitePagesEndpoints
{
    public static RouteGroupBuilder MapSitePages(this RouteGroupBuilder admin)
    {
        var pages = admin.MapGroup("/site-pages")
            .WithTags("Admin site pages")
            .RequireAuthorization(BuiltInSystemPermission.MANAGE_SITE_PAGES_PERMISSION);

        pages.MapGet("", List);
        pages.MapGet("/widget-definitions", WidgetDefinitions);
        pages.MapGet("/{id}", Get);
        pages.MapGet("/{id}/revisions", Revisions);
        pages.MapPost("", Create);
        pages.MapPut("/{id}", Edit);
        pages.MapDelete("/{id}", Delete);
        pages.MapPut("/{id}/settings", EditSettings);
        pages.MapPut("/{id}/widgets", SaveWidgetsHandler);
        pages.MapPut("/{id}/widgets/{widgetId}", EditWidgetHandler);
        pages.MapDelete("/{id}/widgets/{widgetId}", DeleteWidgetHandler);
        pages.MapPost("/{id}/publish", Publish);
        pages.MapPost("/{id}/unpublish", Unpublish);
        pages.MapPost("/{id}/discard-draft", DiscardDraft);
        pages.MapPost("/{id}/set-as-home-page", SetHomePage);
        pages.MapPost("/revisions/{revisionId}/revert", Revert);

        return admin;
    }

    private static async Task<IResult> List([AsParameters] PagedQuery paging, ISender mediator)
    {
        var query = new GetSitePages.Query
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

    private static IResult WidgetDefinitions()
    {
        return Results.Ok(
            WidgetDefinitionService
                .GetAll()
                .Select(d => new
                {
                    developerName = d.DeveloperName,
                    displayName = d.DisplayName,
                    description = d.Description,
                    iconClass = d.IconClass,
                })
        );
    }

    private static async Task<IResult> Get(string id, ISender mediator) =>
        AdminResults.From(await mediator.Send(new GetSitePageById.Query { Id = id }));

    private static async Task<IResult> Revisions(string id, [AsParameters] PagedQuery paging, ISender mediator)
    {
        var query = new GetSitePageRevisionsBySitePageId.Query
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

    private static async Task<IResult> Create([FromBody] CreateSitePage.Command body, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(body), created: true);

    private static async Task<IResult> Edit(string id, [FromBody] EditSitePage.Command body, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(body with { Id = id }));

    private static async Task<IResult> Delete(string id, ISender mediator) =>
        AdminResults.NoContent(await mediator.Send(new DeleteSitePage.Command { Id = id }));

    private static async Task<IResult> EditSettings(
        string id,
        [FromBody] EditSitePageSettings.Command body,
        ISender mediator
    ) => AdminResults.FromId(await mediator.Send(body with { Id = id }));

    private static async Task<IResult> SaveWidgetsHandler(
        string id,
        [FromBody] SaveWidgets.Command body,
        ISender mediator
    ) => AdminResults.FromId(await mediator.Send(body with { Id = id }));

    private static async Task<IResult> EditWidgetHandler(
        string id,
        string widgetId,
        [FromBody] EditWidget.Command body,
        ISender mediator
    ) => AdminResults.FromId(await mediator.Send(body with { SitePageId = id, WidgetId = widgetId }));

    private static async Task<IResult> DeleteWidgetHandler(
        string id,
        string widgetId,
        [FromQuery] string sectionName,
        ISender mediator
    ) =>
        AdminResults.FromId(
            await mediator.Send(
                new DeleteWidget.Command
                {
                    SitePageId = id,
                    WidgetId = widgetId,
                    SectionName = sectionName,
                }
            )
        );

    private static async Task<IResult> Publish(string id, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(new PublishSitePage.Command { Id = id }));

    private static async Task<IResult> Unpublish(string id, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(new UnpublishSitePage.Command { Id = id }));

    private static async Task<IResult> DiscardDraft(string id, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(new DiscardDraftSitePage.Command { Id = id }));

    private static async Task<IResult> SetHomePage(string id, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(new SetSitePageAsHomePage.Command { Id = id }));

    private static async Task<IResult> Revert(string revisionId, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(new RevertSitePage.Command { Id = revisionId }));
}
