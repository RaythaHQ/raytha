using Mediator;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.ContentItems.Commands;
using Raytha.Application.ContentItems.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Utils;

namespace Raytha.Web.Areas.Admin.Api;

/// <summary>
/// Content items under <c>/content-types/{contentTypeDeveloperName}/items</c>. Reads need the
/// content type's <c>read</c> permission, writes need <c>edit</c>.
/// </summary>
public static class ContentItemsEndpoints
{
    private const string Ct = "{" + RouteConstants.CONTENT_TYPE_DEVELOPER_NAME + "}";

    public static RouteGroupBuilder MapContentItems(this RouteGroupBuilder admin)
    {
        var items = admin.MapGroup($"/content-types/{Ct}/items").WithTags("Admin content items");

        var read = items.MapGroup("")
            .RequireAuthorization(BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION);
        read.MapGet("", List);
        read.MapGet("/trash", ListDeleted);
        read.MapGet("/{id}", Get);
        read.MapGet("/{id}/revisions", Revisions);

        var edit = items.MapGroup("")
            .RequireAuthorization(BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION);
        edit.MapPost("", Create);
        edit.MapPut("/{id}", Edit);
        edit.MapDelete("/{id}", Delete);
        edit.MapPut("/{id}/settings", EditSettings);
        edit.MapPost("/{id}/unpublish", Unpublish);
        edit.MapPost("/{id}/discard-draft", DiscardDraft);
        edit.MapPost("/{id}/set-as-home-page", SetHomePage);
        edit.MapPost("/{id}/restore", Restore);
        edit.MapPost("/revisions/{revisionId}/revert", Revert);
        edit.MapDelete("/trash/{id}", DeletePermanently);
        edit.MapPost("/import", ImportFromCsv);

        return admin;
    }

    private static async Task<IResult> List(
        string contentTypeDeveloperName,
        [AsParameters] PagedQuery paging,
        [FromQuery] string? viewId,
        [FromQuery] string? filter,
        ISender mediator
    )
    {
        var query = new GetContentItems.Query
        {
            ContentType = contentTypeDeveloperName,
            ViewId = string.IsNullOrWhiteSpace(viewId) ? null : viewId,
            Filter = filter,
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

    private static async Task<IResult> ListDeleted(
        string contentTypeDeveloperName,
        [AsParameters] PagedQuery paging,
        ISender mediator
    )
    {
        var query = new GetDeletedContentItems.Query
        {
            DeveloperName = contentTypeDeveloperName,
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

    private static async Task<IResult> Get(string contentTypeDeveloperName, string id, ISender mediator) =>
        AdminResults.From(await mediator.Send(new GetContentItemById.Query { Id = id }));

    private static async Task<IResult> Revisions(
        string contentTypeDeveloperName,
        string id,
        [AsParameters] PagedQuery paging,
        ISender mediator
    )
    {
        var query = new GetContentItemRevisionsByContentItemId.Query
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

    private static async Task<IResult> Create(
        string contentTypeDeveloperName,
        [FromBody] CreateContentItem.Command body,
        ISender mediator
    ) =>
        AdminResults.FromId(
            await mediator.Send(body with { ContentTypeDeveloperName = contentTypeDeveloperName }),
            created: true
        );

    private static async Task<IResult> Edit(
        string contentTypeDeveloperName,
        string id,
        [FromBody] EditContentItem.Command body,
        ISender mediator
    ) => AdminResults.FromId(await mediator.Send(body with { Id = id }));

    private static async Task<IResult> Delete(string contentTypeDeveloperName, string id, ISender mediator) =>
        AdminResults.NoContent(await mediator.Send(new DeleteContentItem.Command { Id = id }));

    private static async Task<IResult> EditSettings(
        string contentTypeDeveloperName,
        string id,
        [FromBody] EditContentItemSettings.Command body,
        ISender mediator
    ) => AdminResults.FromId(await mediator.Send(body with { Id = id }));

    private static async Task<IResult> Unpublish(string contentTypeDeveloperName, string id, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(new UnpublishContentItem.Command { Id = id }));

    private static async Task<IResult> DiscardDraft(string contentTypeDeveloperName, string id, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(new DiscardDraftContentItem.Command { Id = id }));

    private static async Task<IResult> SetHomePage(string contentTypeDeveloperName, string id, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(new SetAsHomePage.Command { Id = id }));

    private static async Task<IResult> Restore(string contentTypeDeveloperName, string id, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(new RestoreContentItem.Command { Id = id }));

    private static async Task<IResult> Revert(string contentTypeDeveloperName, string revisionId, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(new RevertContentItem.Command { Id = revisionId }));

    private static async Task<IResult> DeletePermanently(string contentTypeDeveloperName, string id, ISender mediator) =>
        AdminResults.NoContent(await mediator.Send(new DeleteAlreadyDeletedContentItem.Command { Id = id }));

    /// <summary>
    /// Starts <see cref="BeginImportContentItemsFromCsv"/>. Body matches the command:
    /// <c>importMethod</c>, <c>importAsDraft</c>, <c>csvAsBytes</c> (base64).
    /// Returns the background task id.
    /// </summary>
    private static async Task<IResult> ImportFromCsv(
        string contentTypeDeveloperName,
        [FromBody] BeginImportContentItemsFromCsv.Command body,
        ISender mediator
    )
    {
        var contentTypeId = await ContentTypesEndpoints.ResolveContentTypeId(contentTypeDeveloperName, mediator);
        if (contentTypeId is null)
        {
            return Results.NotFound();
        }

        return AdminResults.FromId(await mediator.Send(body with { ContentTypeId = contentTypeId.Value }));
    }
}
