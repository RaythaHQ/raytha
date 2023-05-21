using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Application.MediaItems;
using Raytha.Application.MediaItems.Commands;
using Raytha.Application.MediaItems.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Authentication;
using System.IO;
using System.Threading.Tasks;

namespace Raytha.Web.Areas.Api.Controllers.V1;

[Authorize(Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX + BuiltInSystemPermission.MANAGE_MEDIA_ITEMS)]
public class MediaItemsController : BaseController
{
    [HttpGet("", Name = "GetMediaItems")]
    [Authorize(Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX + BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
    public async Task<ActionResult<IQueryResponseDto<ListResultDto<MediaItemDto>>>> GetMediaItems(
                                           [FromQuery] GetMediaItems.Query request)
    {
        var response = await Mediator.Send(request) as QueryResponseDto<ListResultDto<MediaItemDto>>;
        return response;
    }

    [HttpGet("{objectKey}", Name = "GetMediaItemUrlByObjectKey")]
    public async Task<ActionResult<IQueryResponseDto<MediaItemDto>>> GetMediaItemUrlByObjectKey(
                                        string objectKey)
    {
        var downloadUrl = await FileStorageProvider.GetDownloadUrlAsync(objectKey, FileStorageUtility.GetDefaultExpiry());
        return Ok(new { success = true, result = downloadUrl });
    }

    [HttpPost]
    [Route($"upload-direct", Name = "UploadDirect")]
    public async Task<IActionResult> UploadDirect(IFormFile file)
    {
        if (file.Length <= 0)
        {
            return BadRequest(new { success = false, error = "File length is 0." });
        }
        using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream);
            var data = stream.ToArray();

            var idForKey = ShortGuid.NewGuid();

            var objectKey = FileStorageUtility.CreateObjectKeyFromIdAndFileName(idForKey, file.FileName);
            var downloadUrl = await FileStorageProvider.SaveAndGetDownloadUrlAsync(data, objectKey, file.FileName, file.ContentType, FileStorageUtility.GetDefaultExpiry());

            var input = new CreateMediaItem.Command
            {
                Id = idForKey,
                FileName = file.FileName,
                Length = data.Length,
                ContentType = file.ContentType,
                FileStorageProvider = FileStorageProvider.GetName(),
                ObjectKey = objectKey
            };

            var response = await Mediator.Send(input);
            if (response.Success)
            {
                return CreatedAtAction(nameof(GetMediaItemUrlByObjectKey), new { objectKey }, new { success = true, result = objectKey });
            }
            else
            {
                return BadRequest(response);
            }
        }
    }
}
