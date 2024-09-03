using System;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using Raytha.Application.MediaItems.Commands;
using Raytha.Application.MediaItems.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Views.MediaItems;
using System.IO;
using System.Threading.Tasks;
using Raytha.Application.Themes.Queries;

namespace Raytha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = BuiltInSystemPermission.MANAGE_MEDIA_ITEMS)]
public class MediaItemsController : BaseController
{
    private IRelativeUrlBuilder _relativeUrlBuilder;
    protected IRelativeUrlBuilder RelativeUrlBuilder => _relativeUrlBuilder ??= HttpContext.RequestServices.GetRequiredService<IRelativeUrlBuilder>();

    [HttpPost]
    [Route($"{RAYTHA_ROUTE_PREFIX}/media-items/presign", Name = "mediaitemspresignuploadurl")]
    public async Task<IActionResult> CloudUploadPresignRequest([FromBody] MediaItemPresignRequest_ViewModel body, string contentType)
    {
        var idForKey = ShortGuid.NewGuid();
        var objectKey = FileStorageUtility.CreateObjectKeyFromIdAndFileName(idForKey, body.filename);
        var url = await FileStorageProvider.GetUploadUrlAsync(objectKey, body.filename, body.contentType, FileStorageUtility.GetDefaultExpiry());

        return Json(new { url, fields = new { id = idForKey.ToString(), fileName = body.filename, body.contentType, objectKey } });
    }

    [HttpPost]
    [Route($"{RAYTHA_ROUTE_PREFIX}/media-items/create-after-upload", Name = "mediaitemscreateafterupload")]
    public async Task<IActionResult> CloudUploadCreateAfterUpload([FromBody] MediaItemCreateAfterUpload_ViewModel body, string contentType, string themeId)
    {
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
    public async Task<IActionResult> LocalStorageUpload(IFormFile file, string contentType, string themeId)
    {
        if (file.Length <= 0)
        {
            this.HttpContext.Response.StatusCode = 403;
            return Json(new { success = false });
        }
        using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream);
            var data = stream.ToArray();

            var idForKey = ShortGuid.NewGuid();
            var objectKey = FileStorageUtility.CreateObjectKeyFromIdAndFileName(idForKey, file.FileName);
            await FileStorageProvider.SaveAndGetDownloadUrlAsync(data, objectKey, file.FileName, file.ContentType, FileStorageUtility.GetDefaultExpiry());

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
                return Json(new { url, location = url, success = true, fields = new { id = idForKey.ToString(), fileName = file.FileName, file.ContentType, objectKey } });
            }
            else
            {
                this.HttpContext.Response.StatusCode = 403;
                return Json(new { success = false, error = response.Error });
            }
        }     
    }

    [AllowAnonymous]
    [Route($"{RAYTHA_ROUTE_PREFIX}/media-items/objectkey/{{objectKey}}", Name = "mediaitemsredirecttofileurlbyobjectkey")]
    public IActionResult RedirectToFileUrlByObjectKey(string objectKey)
    {
        var downloadUrl = FileStorageProvider.GetDownloadUrlAsync(objectKey, FileStorageUtility.GetDefaultExpiry()).Result;
        return Redirect(downloadUrl);
    }

    [AllowAnonymous]
    [Route($"{RAYTHA_ROUTE_PREFIX}/media-items/id/{{id}}", Name = "mediaitemsredirecttofileurlbyid")]
    public async Task<IActionResult> RedirectToFileUrlById(string id)
    {
        var input = new GetMediaItemById.Query { Id = id };
        var response = await Mediator.Send(input);

        var downloadUrl = FileStorageProvider.GetDownloadUrlAsync(response.Result.ObjectKey, FileStorageUtility.GetDefaultExpiry()).Result;
        return Redirect(downloadUrl);
    }
}