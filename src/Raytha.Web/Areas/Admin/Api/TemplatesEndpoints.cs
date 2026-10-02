using Mediator;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.EmailTemplates.Commands;
using Raytha.Application.EmailTemplates.Queries;
using Raytha.Application.NavigationMenuItems.Commands;
using Raytha.Application.NavigationMenuItems.Queries;
using Raytha.Application.NavigationMenus.Commands;
using Raytha.Application.NavigationMenus.Queries;
using Raytha.Application.RaythaFunctions.Commands;
using Raytha.Application.RaythaFunctions.Queries;
using Raytha.Domain.Entities;

namespace Raytha.Web.Areas.Admin.Api;

/// <summary>Email templates, navigation menus and Raytha functions.</summary>
public static class TemplatesEndpoints
{
    public static RouteGroupBuilder MapEmailTemplates(this RouteGroupBuilder admin)
    {
        var group = admin.MapGroup("/email-templates")
            .WithTags("Admin email templates")
            .RequireAuthorization(BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION);

        group.MapGet("", ListEmailTemplates);
        group.MapGet("/{id}", GetEmailTemplate);
        group.MapGet("/{id}/revisions", EmailTemplateRevisions);
        group.MapPut("/{id}", EditEmailTemplateHandler);
        group.MapPost("/revisions/{revisionId}/revert", RevertEmailTemplateHandler);

        return admin;
    }

    public static RouteGroupBuilder MapNavigationMenus(this RouteGroupBuilder admin)
    {
        var group = admin.MapGroup("/navigation-menus")
            .WithTags("Admin navigation menus")
            .RequireAuthorization(BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION);

        group.MapGet("", ListMenus);
        group.MapGet("/{id}", GetMenu);
        group.MapGet("/{id}/revisions", MenuRevisions);
        group.MapPost("", CreateMenu);
        group.MapPut("/{id}", EditMenu);
        group.MapDelete("/{id}", DeleteMenu);
        group.MapPost("/{id}/set-main", SetMainMenu);
        group.MapPost("/{id}/revisions", CreateMenuRevision);
        group.MapPost("/revisions/{revisionId}/revert", RevertMenu);

        var items = group.MapGroup("/{id}/items");
        items.MapGet("", ListMenuItems);
        items.MapGet("/{itemId}", GetMenuItem);
        items.MapPost("", CreateMenuItem);
        items.MapPut("/{itemId}", EditMenuItem);
        items.MapDelete("/{itemId}", DeleteMenuItem);
        items.MapPost("/{itemId}/reorder", ReorderMenuItem);

        return admin;
    }

    public static RouteGroupBuilder MapRaythaFunctions(this RouteGroupBuilder admin)
    {
        var group = admin.MapGroup("/functions")
            .WithTags("Admin functions")
            .RequireAuthorization(BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION);

        group.MapGet("", ListFunctions);
        group.MapGet("/{id}", GetFunction);
        group.MapGet("/{id}/revisions", FunctionRevisions);
        group.MapPost("", CreateFunction);
        group.MapPut("/{id}", EditFunction);
        group.MapDelete("/{id}", DeleteFunction);
        group.MapPost("/revisions/{revisionId}/revert", RevertFunction);

        return admin;
    }

    // ---- Email templates ----

    private static async Task<IResult> ListEmailTemplates([AsParameters] PagedQuery paging, ISender mediator)
    {
        var query = new GetEmailTemplates.Query
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

    private static async Task<IResult> GetEmailTemplate(string id, ISender mediator) =>
        AdminResults.From(await mediator.Send(new GetEmailTemplateById.Query { Id = id }));

    private static async Task<IResult> EmailTemplateRevisions(
        string id,
        [AsParameters] PagedQuery paging,
        ISender mediator
    )
    {
        var query = new GetEmailTemplateRevisionsByTemplateId.Query
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

    private static async Task<IResult> EditEmailTemplateHandler(
        string id,
        [FromBody] EditEmailTemplate.Command body,
        ISender mediator
    ) => AdminResults.FromId(await mediator.Send(body with { Id = id }));

    private static async Task<IResult> RevertEmailTemplateHandler(string revisionId, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(new RevertEmailTemplate.Command { Id = revisionId }));

    // ---- Navigation menus ----

    private static async Task<IResult> ListMenus([AsParameters] PagedQuery paging, ISender mediator)
    {
        var query = new GetNavigationMenus.Query
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

    private static async Task<IResult> GetMenu(string id, ISender mediator) =>
        AdminResults.From(await mediator.Send(new GetNavigationMenuById.Query { Id = id }));

    private static async Task<IResult> MenuRevisions(string id, [AsParameters] PagedQuery paging, ISender mediator)
    {
        var query = new GetNavigationMenuRevisionsByNavigationMenuId.Query
        {
            NavigationMenuId = id,
            PageNumber = paging.PageNumber,
            PageSize = paging.PageSize,
        };
        if (paging.HasOrderBy)
        {
            query = query with { OrderBy = paging.OrderBy! };
        }
        return AdminResults.Paged(await mediator.Send(query), paging);
    }

    private static async Task<IResult> CreateMenu([FromBody] CreateNavigationMenu.Command body, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(body), created: true);

    private static async Task<IResult> EditMenu(
        string id,
        [FromBody] EditNavigationMenu.Command body,
        ISender mediator
    ) => AdminResults.FromId(await mediator.Send(body with { Id = id }));

    private static async Task<IResult> DeleteMenu(string id, ISender mediator) =>
        AdminResults.NoContent(await mediator.Send(new DeleteNavigationMenu.Command { Id = id }));

    private static async Task<IResult> SetMainMenu(string id, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(new SetAsMainMenu.Command { Id = id }));

    private static async Task<IResult> CreateMenuRevision(string id, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(new CreateNavigationMenuRevision.Command { NavigationMenuId = id }));

    private static async Task<IResult> RevertMenu(string revisionId, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(new RevertNavigationMenu.Command { Id = revisionId }));

    private static async Task<IResult> ListMenuItems(string id, ISender mediator) =>
        AdminResults.From(
            await mediator.Send(new GetNavigationMenuItemsByNavigationMenuId.Query { NavigationMenuId = id })
        );

    private static async Task<IResult> GetMenuItem(string id, string itemId, ISender mediator) =>
        AdminResults.From(await mediator.Send(new GetNavigationMenuItemById.Query { Id = itemId }));

    private static async Task<IResult> CreateMenuItem(
        string id,
        [FromBody] CreateNavigationMenuItem.Command body,
        ISender mediator
    ) => AdminResults.FromId(await mediator.Send(body with { NavigationMenuId = id }), created: true);

    private static async Task<IResult> EditMenuItem(
        string id,
        string itemId,
        [FromBody] EditNavigationMenuItem.Command body,
        ISender mediator
    ) => AdminResults.FromId(await mediator.Send(body with { Id = itemId, NavigationMenuId = id }));

    private static async Task<IResult> DeleteMenuItem(string id, string itemId, ISender mediator) =>
        AdminResults.NoContent(
            await mediator.Send(new DeleteNavigationMenuItem.Command { Id = itemId, NavigationMenuId = id })
        );

    private static async Task<IResult> ReorderMenuItem(
        string id,
        string itemId,
        [FromBody] ReorderNavigationMenuItems.Command body,
        ISender mediator
    ) => AdminResults.FromId(await mediator.Send(body with { Id = itemId }));

    // ---- Raytha functions ----

    private static async Task<IResult> ListFunctions([AsParameters] PagedQuery paging, ISender mediator)
    {
        var query = new GetRaythaFunctions.Query
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

    private static async Task<IResult> GetFunction(string id, ISender mediator) =>
        AdminResults.From(await mediator.Send(new GetRaythaFunctionById.Query { Id = id }));

    private static async Task<IResult> FunctionRevisions(
        string id,
        [AsParameters] PagedQuery paging,
        ISender mediator
    )
    {
        var query = new GetRaythaFunctionRevisionsByRaythaFunctionId.Query
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

    private static async Task<IResult> CreateFunction(
        [FromBody] CreateRaythaFunction.Command body,
        ISender mediator
    ) => AdminResults.FromId(await mediator.Send(body), created: true);

    private static async Task<IResult> EditFunction(
        string id,
        [FromBody] EditRaythaFunction.Command body,
        ISender mediator
    ) => AdminResults.FromId(await mediator.Send(body with { Id = id }));

    private static async Task<IResult> DeleteFunction(string id, ISender mediator) =>
        AdminResults.NoContent(await mediator.Send(new DeleteRaythaFunction.Command { Id = id }));

    private static async Task<IResult> RevertFunction(string revisionId, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(new RevertRaythaFunction.Command { Id = revisionId }));
}
