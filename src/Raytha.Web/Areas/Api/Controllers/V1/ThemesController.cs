using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Application.MediaItems.Commands;
using Raytha.Application.Themes;
using Raytha.Application.Themes.Commands;
using Raytha.Application.Themes.MediaItems.Queries;
using Raytha.Application.Themes.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Authentication;

namespace Raytha.Web.Areas.Api.Controllers.V1;

public record WidgetFieldTypeResponse(
    string DeveloperName,
    string Label,
    bool HasChoices,
    bool AllowedInRepeater
);

public record ThemeMediaItemResponse(
    string Id,
    string FileName,
    string ContentType,
    long Length,
    string ObjectKey,
    string Url
);

[Authorize(
    Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX
        + BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION
)]
public class ThemesController : BaseController
{
    [HttpGet("", Name = "GetThemes")]
    public async Task<ActionResult<IQueryResponseDto<ListResultDto<ThemeListItemDto>>>> GetThemes(
        string search = "",
        string orderBy = "",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        var input = new GetThemesAsListItems.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
        var response =
            await Mediator.Send(input) as QueryResponseDto<ListResultDto<ThemeListItemDto>>;
        return response;
    }

    [HttpGet("{themeDeveloperName}", Name = "GetThemeByDeveloperName")]
    public async Task<ActionResult<IQueryResponseDto<ThemeDto>>> GetThemeByDeveloperName(
        string themeDeveloperName
    )
    {
        var input = new GetThemeByDeveloperName.Query { DeveloperName = themeDeveloperName };
        var response = await Mediator.Send(input) as QueryResponseDto<ThemeDto>;
        return response;
    }

    [HttpPost("", Name = "CreateTheme")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> CreateTheme(
        [FromBody] CreateTheme.Command request
    )
    {
        var response = await Mediator.Send(request);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return CreatedAtAction(
            nameof(GetThemeByDeveloperName),
            new { themeDeveloperName = request.DeveloperName.ToDeveloperName() },
            response
        );
    }

    [HttpPut("{themeDeveloperName}", Name = "EditTheme")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> EditTheme(
        string themeDeveloperName,
        [FromBody] EditTheme.Command request
    )
    {
        var themeId = await GetThemeId(themeDeveloperName);
        var input = request with { Id = themeId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpDelete("{themeDeveloperName}", Name = "DeleteTheme")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> DeleteTheme(
        string themeDeveloperName
    )
    {
        var themeId = await GetThemeId(themeDeveloperName);
        var input = new DeleteTheme.Command { Id = themeId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpPost("{themeDeveloperName}/set-active", Name = "SetActiveTheme")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> SetActiveTheme(
        string themeDeveloperName
    )
    {
        var themeId = await GetThemeId(themeDeveloperName);
        var input = new SetAsActiveTheme.Command { Id = themeId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpGet("widget-field-types", Name = "GetWidgetFieldTypes")]
    public ActionResult<IQueryResponseDto<IEnumerable<WidgetFieldTypeResponse>>> GetWidgetFieldTypes()
    {
        var fieldTypes = WidgetFieldType
            .SupportedTypes.Select(t => new WidgetFieldTypeResponse(
                t.DeveloperName,
                t.Label,
                t.HasChoices,
                t.AllowedInRepeater
            ))
            .ToArray();
        return new QueryResponseDto<IEnumerable<WidgetFieldTypeResponse>>(fieldTypes);
    }

    [HttpGet("{themeDeveloperName}/media", Name = "GetThemeMedia")]
    public async Task<
        ActionResult<IQueryResponseDto<IEnumerable<ThemeMediaItemResponse>>>
    > GetThemeMedia(string themeDeveloperName, [FromServices] IRelativeUrlBuilder urls)
    {
        var themeId = await GetThemeId(themeDeveloperName);
        var input = new GetMediaItemsByThemeId.Query { ThemeId = themeId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        var items = response
            .Result.Select(m => new ThemeMediaItemResponse(
                m.Id.ToString(),
                m.FileName,
                m.ContentType,
                m.Length,
                m.ObjectKey,
                urls.GetSiteRoot() + urls.MediaRedirectToFilePath(m.ObjectKey)
            ))
            .ToArray();
        return new QueryResponseDto<IEnumerable<ThemeMediaItemResponse>>(items);
    }

    [HttpPost("{themeDeveloperName}/media", Name = "UploadThemeMedia")]
    [Authorize(
        Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX
            + BuiltInSystemPermission.UPLOAD_MEDIA_ITEMS_POLICY
    )]
    public async Task<IActionResult> UploadThemeMedia(
        string themeDeveloperName,
        IFormFile file,
        [FromServices] IRelativeUrlBuilder urls
    )
    {
        var themeId = await GetThemeId(themeDeveloperName);

        if (file.Length <= 0)
        {
            return BadRequest(new { success = false, error = "File length is 0." });
        }

        if (
            !FileStorageUtility.IsAllowedMimeType(
                file.ContentType,
                FileStorageProviderSettings.AllowedMimeTypes
            )
        )
        {
            // Security: Apply the same server-side MIME-type whitelist as the other uploads so that
            // clients cannot upload disallowed content types even if they craft requests manually.
            return BadRequest(new { success = false, error = "File type is not allowed." });
        }

        using var stream = new MemoryStream();
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
        if (!response.Success)
        {
            return BadRequest(response);
        }

        var item = new ThemeMediaItemResponse(
            idForKey.ToString(),
            file.FileName,
            file.ContentType,
            data.Length,
            objectKey,
            urls.GetSiteRoot() + urls.MediaRedirectToFilePath(objectKey)
        );
        return CreatedAtAction(
            nameof(GetThemeMedia),
            new { themeDeveloperName },
            new QueryResponseDto<ThemeMediaItemResponse>(item)
        );
    }

    [HttpDelete("{themeDeveloperName}/media/{id}", Name = "DeleteThemeMedia")]
    [Authorize(
        Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX
            + BuiltInSystemPermission.MANAGE_MEDIA_ITEMS_PERMISSION
    )]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> DeleteThemeMedia(
        string themeDeveloperName,
        string id
    )
    {
        var themeId = await GetThemeId(themeDeveloperName);

        // Only media that belongs to this theme may be deleted through this route.
        var themeMedia = await Mediator.Send(new GetMediaItemsByThemeId.Query { ThemeId = themeId });
        if (!ShortGuid.TryParse(id, out ShortGuid mediaItemId) || themeMedia.Result.All(m => m.Id != mediaItemId))
        {
            throw new NotFoundException("Media Item", id);
        }

        var input = new DeleteMediaItem.Command { Id = mediaItemId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    private async Task<ShortGuid> GetThemeId(string themeDeveloperName)
    {
        var response = await Mediator.Send(
            new GetThemeByDeveloperName.Query { DeveloperName = themeDeveloperName }
        );
        return response.Result.Id;
    }
}
