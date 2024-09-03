using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Models;
using Raytha.Application.NavigationMenuItems;
using Raytha.Application.NavigationMenuItems.Queries;
using Raytha.Application.NavigationMenus;
using Raytha.Application.NavigationMenus.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Authentication;
using Raytha.Web.Utils;

namespace Raytha.Web.Areas.Api.Controllers.V1;

[Authorize(Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX + BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION)]
public class MenusController : BaseController
{
    [HttpGet("", Name = "GetMenus")]
    public async Task<ActionResult<IQueryResponseDto<ListResultDto<NavigationMenuDto>>>> GetMenus(string search = "",
                                                                                                  string orderBy = $"CreationTime {SortOrder.DESCENDING}",
                                                                                                  int pageNumber = 1,
                                                                                                  int pageSize = 50)
    {
        var input = new GetNavigationMenus.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };

        var response = await Mediator.Send(input) as QueryResponseDto<ListResultDto<NavigationMenuDto>>;

        return response;
    }

    [HttpGet($"{{{RouteConstants.MENU_DEVELOPER_NAME}}}", Name = "GetMenuByDeveloperName")]
    public async Task<ActionResult<IQueryResponseDto<NavigationMenuDto>>> GetMenuByDeveloperName(string menuDeveloperName)
    {
        var input = new GetNavigationMenuByDeveloperName.Query
        {
            DeveloperName = menuDeveloperName,
        };

        var response = await Mediator.Send(input) as QueryResponseDto<NavigationMenuDto>;

        return response;
    }

    [HttpGet($"{{{RouteConstants.MENU_DEVELOPER_NAME}}}/menu-items", Name = "GetMenuItemsByMenuDeveloperName")]
    public async Task<ActionResult<IQueryResponseDto<ListResultDto<NavigationMenuItemDto>>>> GetMenuItemsByMenuDeveloperName(string menuDeveloperName)
    {
        var input = new GetNavigationMenuItemsByNavigationMenuDeveloperName.Query
        {
            NavigationMenuDeveloperName = menuDeveloperName,
        };

        var response = await Mediator.Send(input) as QueryResponseDto<ListResultDto<NavigationMenuItemDto>>;

        return response;
    }

    [HttpGet($"{{{RouteConstants.MENU_DEVELOPER_NAME}}}/menu-items/{{menuItemId}}", Name = "GetMenuItemById")]
    public async Task<ActionResult<IQueryResponseDto<NavigationMenuItemDto>>> GetMenuItemById(string menuItemId)
    {
        var input = new GetNavigationMenuItemById.Query
        {
            Id = menuItemId,
        };

        var response = await Mediator.Send(input) as QueryResponseDto<NavigationMenuItemDto>;

        return response;
    }
}