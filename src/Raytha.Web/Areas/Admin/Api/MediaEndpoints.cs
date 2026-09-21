using Mediator;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.MediaItems;
using Raytha.Application.MediaItems.Commands;
using Raytha.Application.MediaItems.Queries;
using Raytha.Domain.Entities;

namespace Raytha.Web.Areas.Admin.Api;

/// <summary>
/// Media library listing. Uploads stay on the existing <c>/raytha/media-items</c> endpoints
/// (multipart / presign), which the SPA already calls directly.
/// </summary>
public static class MediaEndpoints
{
    public static RouteGroupBuilder MapMedia(this RouteGroupBuilder admin)
    {
        // Upload settings are needed by any editor with a file field, not just media managers.
        admin.MapGet("/media/config", Config).WithTags("Admin media");

        var media = admin.MapGroup("/media")
            .WithTags("Admin media")
            .RequireAuthorization(BuiltInSystemPermission.MANAGE_MEDIA_ITEMS);

        media.MapGet("", List);
        media.MapGet("/{id}", Get);
        media.MapDelete("/{id}", Delete);

        return admin;
    }

    private static IResult Config(IFileStorageProviderSettings storage) =>
        Results.Ok(
            new
            {
                useDirectUploadToCloud = storage.UseDirectUploadToCloud,
                fileStorageProvider = storage.FileStorageProvider,
                maxUploadBytes = storage.MaxFileSize,
                allowedMimeTypes = storage.AllowedMimeTypes,
            }
        );

    private static async Task<IResult> List(
        [AsParameters] PagedQuery paging,
        [FromQuery] string? contentType,
        ISender mediator,
        IRelativeUrlBuilder urls
    )
    {
        var query = new GetMediaItems.Query
        {
            ContentType = string.IsNullOrWhiteSpace(contentType) ? null : contentType,
            PageNumber = paging.PageNumber,
            PageSize = paging.PageSize,
            Search = paging.Search,
        };
        if (paging.HasOrderBy)
        {
            query = query with { OrderBy = paging.OrderBy! };
        }
        return AdminResults.Paged(await mediator.Send(query), paging, item => WithUrl(item, urls));
    }

    private static async Task<IResult> Get(string id, ISender mediator, IRelativeUrlBuilder urls) =>
        AdminResults.From(await mediator.Send(new GetMediaItemById.Query { Id = id }), item => WithUrl(item, urls));

    private static async Task<IResult> Delete(string id, ISender mediator) =>
        AdminResults.NoContent(await mediator.Send(new DeleteMediaItem.Command { Id = id }));

    private static object WithUrl(MediaItemDto item, IRelativeUrlBuilder urls)
    {
        return new
        {
            id = item.Id.ToString(),
            fileName = item.FileName,
            contentType = item.ContentType,
            length = item.Length,
            objectKey = item.ObjectKey,
            fileStorageProvider = item.FileStorageProvider,
            creationTime = item.CreationTime,
            url = urls.MediaRedirectToFileUrl(item.ObjectKey),
        };
    }
}
