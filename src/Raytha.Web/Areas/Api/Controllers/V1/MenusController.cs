using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Application.NavigationMenuItems;
using Raytha.Application.NavigationMenuItems.Commands;
using Raytha.Application.NavigationMenuItems.Queries;
using Raytha.Application.NavigationMenus;
using Raytha.Application.NavigationMenus.Commands;
using Raytha.Application.NavigationMenus.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Authentication;
using Raytha.Web.Utils;

namespace Raytha.Web.Areas.Api.Controllers.V1;

[Authorize(
    Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX
        + BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION
)]
[AbsoluteMediaUrls]
public class MenusController : BaseController
{
    [HttpGet("", Name = "GetMenus")]
    public async Task<ActionResult<IQueryResponseDto<ListResultDto<NavigationMenuDto>>>> GetMenus(
        string search = "",
        string orderBy = $"CreationTime {SortOrder.DESCENDING}",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        var input = new GetNavigationMenus.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };

        var response =
            await Mediator.Send(input) as QueryResponseDto<ListResultDto<NavigationMenuDto>>;

        return response;
    }

    [HttpGet($"{{{RouteConstants.MENU_DEVELOPER_NAME}}}", Name = "GetMenuByDeveloperName")]
    public async Task<ActionResult<IQueryResponseDto<NavigationMenuDto>>> GetMenuByDeveloperName(
        string menuDeveloperName
    )
    {
        var input = new GetNavigationMenuByDeveloperName.Query
        {
            DeveloperName = menuDeveloperName,
        };

        var response = await Mediator.Send(input) as QueryResponseDto<NavigationMenuDto>;

        return response;
    }

    [HttpGet(
        $"{{{RouteConstants.MENU_DEVELOPER_NAME}}}/menu-items",
        Name = "GetMenuItemsByMenuDeveloperName"
    )]
    public async Task<
        ActionResult<IQueryResponseDto<IReadOnlyCollection<NavigationMenuItemDto>>>
    > GetMenuItemsByMenuDeveloperName(string menuDeveloperName)
    {
        var input = new GetNavigationMenuItemsByNavigationMenuDeveloperName.Query
        {
            NavigationMenuDeveloperName = menuDeveloperName,
        };

        var response =
            await Mediator.Send(input)
            as QueryResponseDto<IReadOnlyCollection<NavigationMenuItemDto>>;

        return response;
    }

    [HttpGet(
        $"{{{RouteConstants.MENU_DEVELOPER_NAME}}}/menu-items/{{menuItemId}}",
        Name = "GetMenuItemById"
    )]
    public async Task<ActionResult<IQueryResponseDto<NavigationMenuItemDto>>> GetMenuItemById(
        string menuDeveloperName,
        string menuItemId
    )
    {
        var input = new GetNavigationMenuItemById.Query { Id = menuItemId };

        var response = await Mediator.Send(input) as QueryResponseDto<NavigationMenuItemDto>;

        return response;
    }

    [HttpPost("", Name = "CreateMenu")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> CreateMenu(
        [FromBody] CreateNavigationMenu.Command request
    )
    {
        var response = await Mediator.Send(request);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return CreatedAtAction(
            nameof(GetMenuByDeveloperName),
            new { menuDeveloperName = request.DeveloperName.ToDeveloperName() },
            response
        );
    }

    [HttpPut($"{{{RouteConstants.MENU_DEVELOPER_NAME}}}", Name = "EditMenu")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> EditMenu(
        string menuDeveloperName,
        [FromBody] EditNavigationMenu.Command request
    )
    {
        var menuId = await GetMenuId(menuDeveloperName);
        var input = request with { Id = menuId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpDelete($"{{{RouteConstants.MENU_DEVELOPER_NAME}}}", Name = "DeleteMenu")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> DeleteMenu(
        string menuDeveloperName
    )
    {
        var menuId = await GetMenuId(menuDeveloperName);
        var input = new DeleteNavigationMenu.Command { Id = menuId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpPost($"{{{RouteConstants.MENU_DEVELOPER_NAME}}}/set-main", Name = "SetMainMenu")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> SetMainMenu(
        string menuDeveloperName
    )
    {
        var menuId = await GetMenuId(menuDeveloperName);
        var input = new SetAsMainMenu.Command { Id = menuId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpPost($"{{{RouteConstants.MENU_DEVELOPER_NAME}}}/menu-items", Name = "CreateMenuItem")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> CreateMenuItem(
        string menuDeveloperName,
        [FromBody] CreateNavigationMenuItem.Command request
    )
    {
        var menuId = await GetMenuId(menuDeveloperName);
        var input = request with { NavigationMenuId = menuId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return CreatedAtAction(
            nameof(GetMenuItemById),
            new { menuDeveloperName, menuItemId = response.Result },
            response
        );
    }

    [HttpPut(
        $"{{{RouteConstants.MENU_DEVELOPER_NAME}}}/menu-items/{{menuItemId}}",
        Name = "EditMenuItem"
    )]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> EditMenuItem(
        string menuDeveloperName,
        string menuItemId,
        [FromBody] EditNavigationMenuItem.Command request
    )
    {
        var menuId = await GetMenuId(menuDeveloperName);
        var input = request with { Id = menuItemId, NavigationMenuId = menuId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpDelete(
        $"{{{RouteConstants.MENU_DEVELOPER_NAME}}}/menu-items/{{menuItemId}}",
        Name = "DeleteMenuItem"
    )]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> DeleteMenuItem(
        string menuDeveloperName,
        string menuItemId
    )
    {
        var menuId = await GetMenuId(menuDeveloperName);
        var input = new DeleteNavigationMenuItem.Command
        {
            Id = menuItemId,
            NavigationMenuId = menuId,
        };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpPost(
        $"{{{RouteConstants.MENU_DEVELOPER_NAME}}}/menu-items/{{menuItemId}}/reorder",
        Name = "ReorderMenuItem"
    )]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> ReorderMenuItem(
        string menuDeveloperName,
        string menuItemId,
        [FromBody] ReorderNavigationMenuItems.Command request
    )
    {
        await GetMenuId(menuDeveloperName);
        var input = request with { Id = menuItemId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    private async Task<ShortGuid> GetMenuId(string menuDeveloperName)
    {
        var response = await Mediator.Send(
            new GetNavigationMenuByDeveloperName.Query { DeveloperName = menuDeveloperName }
        );
        return response.Result.Id;
    }
}
