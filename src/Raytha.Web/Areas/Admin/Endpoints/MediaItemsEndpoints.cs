using System.IO;
using CSharpVitamins;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using Raytha.Application.MediaItems.Commands;
using Raytha.Application.MediaItems.Queries;
using Raytha.Domain.Entities;

namespace Raytha.Web.Areas.Admin.Endpoints;

public static class MediaItemsEndpoints
{
    private const string RAYTHA_ROUTE_PREFIX = "raytha";

    public static IEndpointRouteBuilder MapMediaItemsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup($"/{RAYTHA_ROUTE_PREFIX}/media-items");

        group
            .MapPost("/presign", CloudUploadPresignRequest)
            .WithName("mediaitemspresignuploadurl")
            .RequireAuthorization(BuiltInSystemPermission.MANAGE_MEDIA_ITEMS);

        group
            .MapPost("/create-after-upload", CloudUploadCreateAfterUpload)
            .WithName("mediaitemscreateafterupload")
            .RequireAuthorization(BuiltInSystemPermission.MANAGE_MEDIA_ITEMS);

        group
            .MapPost("/upload", DirectUpload)
            .WithName("mediaitemslocalstorageupload")
            .RequireAuthorization(BuiltInSystemPermission.MANAGE_MEDIA_ITEMS)
            .DisableAntiforgery();

        group
            .MapGet("/objectkey/{objectKey}", RedirectToFileUrlByObjectKey)
            .WithName("mediaitemsredirecttofileurlbyobjectkey")
            .AllowAnonymous();

        group
            .MapGet("/id/{id}", RedirectToFileUrlById)
            .WithName("mediaitemsredirecttofileurlbyid")
            .AllowAnonymous();

        return endpoints;
    }

    private static async Task<IResult> CloudUploadPresignRequest(
        [FromBody] MediaItemPresignRequest body,
        [FromServices] IFileStorageProvider fileStorageProvider,
        [FromServices] IFileStorageProviderSettings fileStorageProviderSettings
    )
    {
        if (
            !FileStorageUtility.IsAllowedMimeType(
                body.ContentType,
                fileStorageProviderSettings.AllowedMimeTypes
            )
        )
        {
            // Security: Enforce server-side MIME-type checks so clients cannot bypass front-end
            // validation to upload disallowed file types; this is safe because it only rejects
            // types already disallowed by configuration and returns a clear JSON error.
            return Results.Json(
                new { success = false, error = "File type is not allowed." },
                statusCode: StatusCodes.Status403Forbidden
            );
        }

        var idForKey = ShortGuid.NewGuid();
        var objectKey = FileStorageUtility.CreateObjectKeyFromIdAndFileName(
            idForKey,
            body.Filename
        );
        var url = await fileStorageProvider.GetUploadUrlAsync(
            objectKey,
            body.Filename,
            body.ContentType,
            FileStorageUtility.GetDefaultExpiry()
        );

        return Results.Json(
            new
            {
                url,
                fields = new
                {
                    id = idForKey.ToString(),
                    fileName = body.Filename,
                    contentType = body.ContentType,
                    objectKey,
                },
            }
        );
    }

    private static async Task<IResult> CloudUploadCreateAfterUpload(
        [FromBody] MediaItemCreateAfterUpload body,
        [FromQuery] string? themeId,
        [FromServices] ISender mediator,
        [FromServices] IFileStorageProvider fileStorageProvider,
        [FromServices] IFileStorageProviderSettings fileStorageProviderSettings
    )
    {
        if (
            !FileStorageUtility.IsAllowedMimeType(
                body.ContentType,
                fileStorageProviderSettings.AllowedMimeTypes
            )
        )
        {
            // Security: Double-check MIME types on the post-upload finalize step to ensure only
            // previously approved file types are persisted in the media library.
            return Results.Json(
                new { success = false, error = "File type is not allowed." },
                statusCode: StatusCodes.Status403Forbidden
            );
        }

        var input = new CreateMediaItem.Command
        {
            Id = body.Id,
            FileName = body.Filename,
            Length = body.Length,
            ContentType = body.ContentType,
            FileStorageProvider = fileStorageProvider.GetName(),
            ObjectKey = body.ObjectKey,
            ThemeId = themeId,
        };

        var response = await mediator.Send(input);
        if (response.Success)
        {
            return Results.Json(new { success = true });
        }
        else
        {
            return Results.Json(
                new { success = false, error = response.Error },
                statusCode: StatusCodes.Status403Forbidden
            );
        }
    }

    private static async Task<IResult> DirectUpload(
        IFormFile file,
        [FromQuery] string? themeId,
        [FromServices] ISender mediator,
        [FromServices] IFileStorageProvider fileStorageProvider,
        [FromServices] IFileStorageProviderSettings fileStorageProviderSettings,
        [FromServices] IRelativeUrlBuilder relativeUrlBuilder
    )
    {
        if (file.Length <= 0)
        {
            return Results.Json(
                new { success = false },
                statusCode: StatusCodes.Status403Forbidden
            );
        }

        if (
            !FileStorageUtility.IsAllowedMimeType(
                file.ContentType,
                fileStorageProviderSettings.AllowedMimeTypes
            )
        )
        {
            // Security: Prevent local storage uploads of disallowed MIME types regardless of any
            // client-side checks, reducing the chance of storing unexpected executable or HTML content.
            return Results.Json(
                new { success = false, error = "File type is not allowed." },
                statusCode: StatusCodes.Status403Forbidden
            );
        }

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        var data = stream.ToArray();

        var idForKey = ShortGuid.NewGuid();
        var objectKey = FileStorageUtility.CreateObjectKeyFromIdAndFileName(
            idForKey,
            file.FileName
        );
        await fileStorageProvider.SaveAndGetDownloadUrlAsync(
            data,
            objectKey,
            file.FileName,
            file.ContentType,
            FileStorageUtility.GetDefaultExpiry()
        );

        var input = new CreateMediaItem.Command
        {
            Id = idForKey,
            FileName = file.FileName,
            Length = data.Length,
            ContentType = file.ContentType,
            FileStorageProvider = fileStorageProvider.GetName(),
            ObjectKey = objectKey,
            ThemeId = themeId,
        };

        var response = await mediator.Send(input);
        if (response.Success)
        {
            var url = relativeUrlBuilder.MediaRedirectToFileUrl(objectKey);
            return Results.Json(
                new
                {
                    url,
                    location = url,
                    success = true,
                    fields = new
                    {
                        id = idForKey.ToString(),
                        fileName = file.FileName,
                        contentType = file.ContentType,
                        objectKey,
                    },
                }
            );
        }
        else
        {
            return Results.Json(
                new { success = false, error = response.Error },
                statusCode: StatusCodes.Status403Forbidden
            );
        }
    }

    private static async Task<IResult> RedirectToFileUrlByObjectKey(
        string objectKey,
        [FromServices] IFileStorageProvider fileStorageProvider
    )
    {
        var downloadUrl = await fileStorageProvider.GetDownloadUrlAsync(
            objectKey,
            FileStorageUtility.GetDefaultExpiry()
        );
        return Results.Redirect(downloadUrl);
    }

    private static async Task<IResult> RedirectToFileUrlById(
        string id,
        [FromServices] ISender mediator,
        [FromServices] IFileStorageProvider fileStorageProvider
    )
    {
        var input = new GetMediaItemById.Query { Id = id };
        var response = await mediator.Send(input);

        var downloadUrl = await fileStorageProvider.GetDownloadUrlAsync(
            response.Result.ObjectKey,
            FileStorageUtility.GetDefaultExpiry()
        );
        return Results.Redirect(downloadUrl);
    }
}

// DTOs for request bodies
public record MediaItemPresignRequest(string Filename, string ContentType, string? Extension);

public record MediaItemCreateAfterUpload(
    string Id,
    string Filename,
    string ContentType,
    string? Extension,
    string ObjectKey,
    long Length,
    string? ContentDisposition
);
