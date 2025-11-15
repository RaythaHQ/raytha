using System;
using System.IO;
using System.Threading.Tasks;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using Raytha.Application.MediaItems.Commands;
using Raytha.Application.MediaItems.Queries;
using Raytha.Application.Themes.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Views.MediaItems;

namespace Raytha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = BuiltInSystemPermission.MANAGE_MEDIA_ITEMS)]
public class MediaItemsController : BaseController
{
    private IRelativeUrlBuilder _relativeUrlBuilder;
    protected IRelativeUrlBuilder RelativeUrlBuilder =>
        _relativeUrlBuilder ??=
            HttpContext.RequestServices.GetRequiredService<IRelativeUrlBuilder>();

    [HttpPost]
    [Route($"{RAYTHA_ROUTE_PREFIX}/media-items/presign", Name = "mediaitemspresignuploadurl")]
    public async Task<IActionResult> CloudUploadPresignRequest(
        [FromBody] MediaItemPresignRequest_ViewModel body,
        string contentType
    )
    {
        if (
            !FileStorageUtility.IsAllowedMimeType(
                body.contentType,
                FileStorageProviderSettings.AllowedMimeTypes
            )
        )
        {
            // Security: Enforce server-side MIME-type checks so clients cannot bypass front-end
            // validation to upload disallowed file types; this is safe because it only rejects
            // types already disallowed by configuration and returns a clear JSON error.
            this.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Json(new { success = false, error = "File type is not allowed." });
        }

        var idForKey = ShortGuid.NewGuid();
        var objectKey = FileStorageUtility.CreateObjectKeyFromIdAndFileName(
            idForKey,
            body.filename
        );
        var url = await FileStorageProvider.GetUploadUrlAsync(
            objectKey,
            body.filename,
            body.contentType,
            FileStorageUtility.GetDefaultExpiry()
        );

        return Json(
            new
            {
                url,
                fields = new
                {
                    id = idForKey.ToString(),
                    fileName = body.filename,
                    body.contentType,
                    objectKey,
                },
            }
        );
    }

    [HttpPost]
    [Route(
        $"{RAYTHA_ROUTE_PREFIX}/media-items/create-after-upload",
        Name = "mediaitemscreateafterupload"
    )]
    public async Task<IActionResult> CloudUploadCreateAfterUpload(
        [FromBody] MediaItemCreateAfterUpload_ViewModel body,
        string contentType,
        string themeId
    )
    {
        if (
            !FileStorageUtility.IsAllowedMimeType(
                body.contentType,
                FileStorageProviderSettings.AllowedMimeTypes
            )
        )
        {
            // Security: Double-check MIME types on the post-upload finalize step to ensure only
            // previously approved file types are persisted in the media library.
            this.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Json(new { success = false, error = "File type is not allowed." });
        }

        var input = new CreateMediaItem.Command
        {
            Id = body.id,
            FileName = body.filename,
            Length = body.length,
            ContentType = body.contentType,
            FileStorageProvider = FileStorageProvider.GetName(),
            ObjectKey = body.objectKey,
            ThemeId = themeId,
        };

        var response = await Mediator.Send(input);
        if (response.Success)
        {
            return Json(new { success = true });
        }
        else
        {
            this.HttpContext.Response.StatusCode = 403;
            return Json(new { success = false, error = response.Error });
        }
    }

    [HttpPost]
    [Route($"{RAYTHA_ROUTE_PREFIX}/media-items/upload", Name = "mediaitemslocalstorageupload")]
    public async Task<IActionResult> DirectUpload(
        IFormFile file,
        string contentType,
        string themeId
    )
    {
        if (file.Length <= 0)
        {
            this.HttpContext.Response.StatusCode = 403;
            return Json(new { success = false });
        }

        if (
            !FileStorageUtility.IsAllowedMimeType(
                file.ContentType,
                FileStorageProviderSettings.AllowedMimeTypes
            )
        )
        {
            // Security: Prevent local storage uploads of disallowed MIME types regardless of any
            // client-side checks, reducing the chance of storing unexpected executable or HTML content.
            this.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Json(new { success = false, error = "File type is not allowed." });
        }
        using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream);
            var data = stream.ToArray();

            var idForKey = ShortGuid.NewGuid();
            var objectKey = FileStorageUtility.CreateObjectKeyFromIdAndFileName(
                idForKey,
                file.FileName
            );
            await FileStorageProvider.SaveAndGetDownloadUrlAsync(
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
                FileStorageProvider = FileStorageProvider.GetName(),
                ObjectKey = objectKey,
                ThemeId = themeId,
            };

            var response = await Mediator.Send(input);
            if (response.Success)
            {
                var url = RelativeUrlBuilder.MediaRedirectToFileUrl(objectKey);
                return Json(
                    new
                    {
                        url,
                        location = url,
                        success = true,
                        fields = new
                        {
                            id = idForKey.ToString(),
                            fileName = file.FileName,
                            file.ContentType,
                            objectKey,
                        },
                    }
                );
            }
            else
            {
                this.HttpContext.Response.StatusCode = 403;
                return Json(new { success = false, error = response.Error });
            }
        }
    }

    [AllowAnonymous]
    [Route(
        $"{RAYTHA_ROUTE_PREFIX}/media-items/objectkey/{{objectKey}}",
        Name = "mediaitemsredirecttofileurlbyobjectkey"
    )]
    public IActionResult RedirectToFileUrlByObjectKey(string objectKey)
    {
        var downloadUrl = FileStorageProvider
            .GetDownloadUrlAsync(objectKey, FileStorageUtility.GetDefaultExpiry())
            .Result;
        return Redirect(downloadUrl);
    }

    [AllowAnonymous]
    [Route(
        $"{RAYTHA_ROUTE_PREFIX}/media-items/id/{{id}}",
        Name = "mediaitemsredirecttofileurlbyid"
    )]
    public async Task<IActionResult> RedirectToFileUrlById(string id)
    {
        var input = new GetMediaItemById.Query { Id = id };
        var response = await Mediator.Send(input);

        var downloadUrl = FileStorageProvider
            .GetDownloadUrlAsync(response.Result.ObjectKey, FileStorageUtility.GetDefaultExpiry())
            .Result;
        return Redirect(downloadUrl);
    }
}
