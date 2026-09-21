using CSharpVitamins;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Security;
using Raytha.Application.ContentTypes.Commands;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Application.Themes.WebTemplates.Queries;
using Raytha.Application.Views.Commands;
using Raytha.Application.Views.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Web.Utils;

namespace Raytha.Web.Areas.Admin.Api;

/// <summary>
/// Content types plus their fields and views. Every content-type-scoped route carries
/// <c>{contentTypeDeveloperName}</c> because <c>RaythaAdminAuthorizationHandler</c> reads that
/// route value to evaluate per-content-type permissions.
/// </summary>
public static class ContentTypesEndpoints
{
    private const string Ct = "{" + RouteConstants.CONTENT_TYPE_DEVELOPER_NAME + "}";

    public static RouteGroupBuilder MapContentTypes(this RouteGroupBuilder admin)
    {
        var types = admin.MapGroup("/content-types").WithTags("Admin content types");

        types.MapGet("", ListContentTypes).RequireAuthorization(RaythaClaimTypes.IsAdmin);
        types.MapGet("/field-types", FieldTypes).RequireAuthorization(RaythaClaimTypes.IsAdmin);
        types.MapPost("", CreateContentTypeHandler)
            .RequireAuthorization(BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION);
        types.MapGet($"/{Ct}", GetContentType)
            .RequireAuthorization(BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION);
        types.MapPut($"/{Ct}", EditContentTypeHandler)
            .RequireAuthorization(BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION);
        types.MapGet($"/{Ct}/templates", ContentTypeTemplates)
            .RequireAuthorization(BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION);

        var fields = types.MapGroup($"/{Ct}/fields")
            .RequireAuthorization(BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION);
        fields.MapGet("", ListFields);
        fields.MapGet("/{id}", GetField);
        fields.MapPost("", CreateField);
        fields.MapPut("/{id}", EditField);
        fields.MapDelete("/{id}", DeleteField);
        fields.MapPost("/{id}/reorder", ReorderField);

        var views = types.MapGroup($"/{Ct}/views");
        views.MapGet("", ListViews)
            .RequireAuthorization(BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION);
        views.MapGet("/favorites", FavoriteViews)
            .RequireAuthorization(BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION);
        views.MapGet("/{id}", GetView)
            .RequireAuthorization(BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION);
        views.MapPost("/{id}/favorite", ToggleFavorite)
            .RequireAuthorization(BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION);
        views.MapPost("/{id}/export", ExportViewCsv)
            .RequireAuthorization(BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION);

        var viewConfig = views.MapGroup("")
            .RequireAuthorization(BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION);
        viewConfig.MapPost("", CreateViewHandler);
        viewConfig.MapPut("/{id}", EditViewHandler);
        viewConfig.MapDelete("/{id}", DeleteViewHandler);
        viewConfig.MapPut("/{id}/columns", EditColumnHandler);
        viewConfig.MapPost("/{id}/columns/reorder", ReorderColumnHandler);
        viewConfig.MapPut("/{id}/sort", EditSortHandler);
        viewConfig.MapPost("/{id}/sort/reorder", ReorderSortHandler);
        viewConfig.MapPut("/{id}/filter", EditFilterHandler);
        viewConfig.MapPut("/{id}/public-settings", EditPublicSettingsHandler);
        viewConfig.MapPost("/{id}/set-as-home-page", SetViewAsHomePage);

        return admin;
    }

    internal static async Task<ShortGuid?> ResolveContentTypeId(string developerName, ISender mediator)
    {
        var response = await mediator.Send(
            new GetContentTypeByDeveloperName.Query { DeveloperName = developerName }
        );
        return response.Success ? response.Result.Id : null;
    }

    private static async Task<IResult> ListContentTypes([AsParameters] PagedQuery paging, ISender mediator)
    {
        var query = new GetContentTypes.Query
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

    private static IResult FieldTypes()
    {
        return Results.Ok(
            BaseFieldType.SupportedTypes.Select(t => new { label = t.Label, developerName = t.DeveloperName })
        );
    }

    private static async Task<IResult> GetContentType(string contentTypeDeveloperName, ISender mediator) =>
        AdminResults.From(
            await mediator.Send(new GetContentTypeByDeveloperName.Query { DeveloperName = contentTypeDeveloperName })
        );

    private static async Task<IResult> CreateContentTypeHandler(
        [FromBody] CreateContentType.Command body,
        ISender mediator
    ) => AdminResults.FromId(await mediator.Send(body), created: true);

    private static async Task<IResult> EditContentTypeHandler(
        string contentTypeDeveloperName,
        [FromBody] EditContentType.Command body,
        ISender mediator
    )
    {
        var id = await ResolveContentTypeId(contentTypeDeveloperName, mediator);
        if (id is null)
        {
            return Results.NotFound();
        }
        return AdminResults.FromId(await mediator.Send(body with { Id = id.Value }));
    }

    /// <summary>Web templates in the active theme that a content item of this type may use.</summary>
    private static async Task<IResult> ContentTypeTemplates(
        string contentTypeDeveloperName,
        ISender mediator,
        ICurrentOrganization organization
    )
    {
        var id = await ResolveContentTypeId(contentTypeDeveloperName, mediator);
        if (id is null)
        {
            return Results.NotFound();
        }

        var response = await mediator.Send(
            new GetWebTemplates.Query
            {
                ThemeId = organization.ActiveThemeId,
                ContentTypeId = id,
                PageSize = int.MaxValue,
            }
        );
        return AdminResults.From(
            response,
            list => list.Items.Select(t => new { id = t.Id.ToString(), label = t.Label, developerName = t.DeveloperName })
        );
    }

    private static async Task<IResult> ListFields(
        string contentTypeDeveloperName,
        [AsParameters] PagedQuery paging,
        [FromQuery] bool? deleted,
        ISender mediator
    )
    {
        var query = new GetContentTypeFields.Query
        {
            DeveloperName = contentTypeDeveloperName,
            ShowDeletedOnly = deleted ?? false,
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

    private static async Task<IResult> GetField(string contentTypeDeveloperName, string id, ISender mediator) =>
        AdminResults.From(await mediator.Send(new GetContentTypeFieldById.Query { Id = id }));

    private static async Task<IResult> CreateField(
        string contentTypeDeveloperName,
        [FromBody] CreateContentTypeField.Command body,
        ISender mediator
    )
    {
        var contentTypeId = await ResolveContentTypeId(contentTypeDeveloperName, mediator);
        if (contentTypeId is null)
        {
            return Results.NotFound();
        }
        return AdminResults.FromId(await mediator.Send(body with { ContentTypeId = contentTypeId.Value }), created: true);
    }

    private static async Task<IResult> EditField(
        string contentTypeDeveloperName,
        string id,
        [FromBody] EditContentTypeField.Command body,
        ISender mediator
    ) => AdminResults.FromId(await mediator.Send(body with { Id = id }));

    private static async Task<IResult> DeleteField(string contentTypeDeveloperName, string id, ISender mediator) =>
        AdminResults.NoContent(await mediator.Send(new DeleteContentTypeField.Command { Id = id }));

    private static async Task<IResult> ReorderField(
        string contentTypeDeveloperName,
        string id,
        [FromBody] ReorderRequest body,
        ISender mediator
    ) =>
        AdminResults.FromId(
            await mediator.Send(new ReorderContentTypeField.Command { Id = id, NewFieldOrder = body.NewFieldOrder })
        );

    private static async Task<IResult> ListViews(
        string contentTypeDeveloperName,
        [AsParameters] PagedQuery paging,
        ISender mediator
    )
    {
        var query = new GetViews.Query
        {
            ContentTypeDeveloperName = contentTypeDeveloperName,
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

    private static async Task<IResult> FavoriteViews(
        string contentTypeDeveloperName,
        [AsParameters] PagedQuery paging,
        ISender mediator,
        ICurrentUser currentUser
    )
    {
        var query = new GetFavoriteViewsForAdmin.Query
        {
            ContentTypeDeveloperName = contentTypeDeveloperName,
            UserId = currentUser.UserId ?? ShortGuid.Empty,
            PageNumber = paging.PageNumber,
            PageSize = paging.PageSize,
        };
        return AdminResults.Paged(await mediator.Send(query), paging);
    }

    private static async Task<IResult> GetView(string contentTypeDeveloperName, string id, ISender mediator) =>
        AdminResults.From(await mediator.Send(new GetViewById.Query { Id = id }));

    private static async Task<IResult> ToggleFavorite(
        string contentTypeDeveloperName,
        string id,
        [FromBody] FavoriteRequest body,
        ISender mediator,
        ICurrentUser currentUser
    ) =>
        AdminResults.FromId(
            await mediator.Send(
                new ToggleViewAsFavoriteForAdmin.Command
                {
                    ViewId = id,
                    UserId = currentUser.UserId ?? ShortGuid.Empty,
                    SetAsFavorite = body.SetAsFavorite,
                }
            )
        );

    private static async Task<IResult> CreateViewHandler(
        string contentTypeDeveloperName,
        [FromBody] CreateView.Command body,
        ISender mediator
    )
    {
        var contentTypeId = await ResolveContentTypeId(contentTypeDeveloperName, mediator);
        if (contentTypeId is null)
        {
            return Results.NotFound();
        }
        return AdminResults.FromId(await mediator.Send(body with { ContentTypeId = contentTypeId.Value }), created: true);
    }

    private static async Task<IResult> EditViewHandler(
        string contentTypeDeveloperName,
        string id,
        [FromBody] EditView.Command body,
        ISender mediator
    ) => AdminResults.FromId(await mediator.Send(body with { Id = id }));

    private static async Task<IResult> DeleteViewHandler(string contentTypeDeveloperName, string id, ISender mediator) =>
        AdminResults.NoContent(await mediator.Send(new DeleteView.Command { Id = id }));

    private static async Task<IResult> EditColumnHandler(
        string contentTypeDeveloperName,
        string id,
        [FromBody] EditColumn.Command body,
        ISender mediator
    ) => AdminResults.FromId(await mediator.Send(body with { Id = id }));

    private static async Task<IResult> ReorderColumnHandler(
        string contentTypeDeveloperName,
        string id,
        [FromBody] ReorderColumn.Command body,
        ISender mediator
    ) => AdminResults.FromId(await mediator.Send(body with { Id = id }));

    private static async Task<IResult> EditSortHandler(
        string contentTypeDeveloperName,
        string id,
        [FromBody] EditSort.Command body,
        ISender mediator
    ) => AdminResults.FromId(await mediator.Send(body with { Id = id }));

    private static async Task<IResult> ReorderSortHandler(
        string contentTypeDeveloperName,
        string id,
        [FromBody] ReorderSort.Command body,
        ISender mediator
    ) => AdminResults.FromId(await mediator.Send(body with { Id = id }));

    private static async Task<IResult> EditFilterHandler(
        string contentTypeDeveloperName,
        string id,
        [FromBody] EditFilter.Command body,
        ISender mediator
    ) => AdminResults.FromId(await mediator.Send(body with { Id = id }));

    private static async Task<IResult> EditPublicSettingsHandler(
        string contentTypeDeveloperName,
        string id,
        [FromBody] EditPublicSettings.Command body,
        ISender mediator
    ) => AdminResults.FromId(await mediator.Send(body with { Id = id }));

    private static async Task<IResult> SetViewAsHomePage(string contentTypeDeveloperName, string id, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(new SetAsHomePage.Command { Id = id }));

    /// <summary>
    /// Starts <see cref="BeginExportContentItemsToCsv"/>. Body matches the command:
    /// <c>exportOnlyColumnsFromView</c>. Returns the background task id; when the task
    /// finishes, <c>statusInfo</c> is a serialized media item for download.
    /// </summary>
    private static async Task<IResult> ExportViewCsv(
        string contentTypeDeveloperName,
        string id,
        [FromBody] Raytha.Application.ContentItems.Commands.BeginExportContentItemsToCsv.Command body,
        ISender mediator
    ) => AdminResults.FromId(await mediator.Send(body with { ViewId = id }));

    public sealed record ReorderRequest(int NewFieldOrder);

    public sealed record FavoriteRequest(bool SetAsFavorite);
}
