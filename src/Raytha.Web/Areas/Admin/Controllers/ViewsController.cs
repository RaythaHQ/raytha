using CSharpVitamins;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Domain.ValueObjects;
using Raytha.Application.Views;
using Raytha.Application.Views.Queries;
using Raytha.Application.Views.Commands;
using Raytha.Web.Filters;
using System.Linq;
using Raytha.Web.Areas.Admin.Views.Views;
using System.Collections.Generic;
using Raytha.Domain.Entities;
using System;
using System.Text.Json;
using System.Net;
using Raytha.Domain.ValueObjects.FieldTypes;
using Microsoft.AspNetCore.Authorization;
using Raytha.Application.Common.Utils;
using Raytha.Web.Utils;
using Raytha.Application.Themes.WebTemplates.Queries;

namespace Raytha.Web.Areas.Admin.Controllers;

public class ViewsController : BaseController
{
    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION)]
    [ServiceFilter(typeof(SetPaginationInformationFilterAttribute))]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views", Name = "viewsindex")]
    public async Task<IActionResult> Index(string search = "", string orderBy = $"Label {SortOrder.ASCENDING}", int pageNumber = 1, int pageSize = 50)
    {
        var favoriteViewsForAdmin = await Mediator.Send(new GetFavoriteViewsForAdmin.Query { UserId = CurrentUser.UserId.Value, ContentTypeId = CurrentView.ContentTypeId });

        var input = new GetViews.Query
        {
            ContentTypeDeveloperName = CurrentView.ContentType.DeveloperName,
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy
        };

        var response = await Mediator.Send(input);

        var items = response.Result.Items.Select(p => new ViewsListItem_ViewModel
        {
            Id = p.Id,
            Label = p.Label,
            DeveloperName = p.DeveloperName,
            LastModificationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(p.LastModificationTime),
            LastModifierUser = p.LastModifierUser != null ? p.LastModifierUser.FullName : "N/A",
            IsPublished = p.IsPublished.YesOrNo(),
            RoutePath = p.RoutePath,
            IsFavoriteForAdmin = favoriteViewsForAdmin.Result.Items.Any(c => c.Id == p.Id),
            IsHomePage = CurrentOrganization.HomePageId == p.Id
        });

        var viewModel = new ViewsPagination_ViewModel(items, response.Result.TotalCount);
        return View(viewModel);
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/create", Name = "viewscreate")]
    public async Task<IActionResult> Create(string duplicateFromId)
    {
        var response = await Mediator.Send(new GetContentTypeByDeveloperName.Query { DeveloperName = CurrentView.ContentType.DeveloperName });

        ViewDto duplicatedView = null;

        if (!string.IsNullOrEmpty(duplicateFromId))
        {
            duplicatedView = (await Mediator.Send(new GetViewById.Query { Id = duplicateFromId })).Result;
        }

        var viewModel = new ViewsCreate_ViewModel
        {
            ContentTypeId = response.Result.Id,
            Label = duplicatedView != null ? duplicatedView.Label + " (duplicate)" : string.Empty,
            DeveloperName = duplicatedView != null ? duplicatedView.DeveloperName + "_duplicate" : string.Empty,
            Description = duplicatedView != null ? duplicatedView.Description : string.Empty,
            DuplicateFromId = duplicatedView?.Id
        };

        return View(viewModel);
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/create", Name = "viewscreate")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ViewsCreate_ViewModel model)
    {
        var input = new CreateView.Command
        {
            Label = model.Label,
            DeveloperName = model.DeveloperName,
            ContentTypeId = model.ContentTypeId,
            DuplicateFromId = model.DuplicateFromId,
            Description = model.Description
        };
        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage($"{model.Label} created successfully.");
            ShortGuid newViewId = response.Result;
            return RedirectToAction("Index", "ContentItems", new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName, viewId = newViewId });
        }
        else
        {
            SetErrorMessage("There were validation errors with your form submission. Please correct the fields below.", response.GetErrors());
            return View(model);
        }
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/edit/{{viewId}}", Name = "viewsedit")]
    public async Task<IActionResult> Edit()
    {
        var response = await Mediator.Send(new GetViewById.Query { Id = CurrentView.Id });

        var viewModel = new ViewsEdit_ViewModel
        {
            Label = response.Result.Label,
            DeveloperName = response.Result.DeveloperName,
            Description = response.Result.Description,
            Id = response.Result.Id
        };

        return View(viewModel);
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/edit/{{viewId}}", Name = "viewsedit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ViewsEdit_ViewModel model)
    {
        var input = new EditView.Command
        {
            Label = model.Label,
            Id = CurrentView.Id,
            Description = model.Description
        };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{model.Label} edit successfully.");
            return RedirectToAction("Index", "ContentItems", new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName, id = CurrentView.Id });
        }
        else
        {
            SetErrorMessage("There were validation errors with your form submission. Please correct the fields below.", response.GetErrors());
            return View(model);
        }
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/public-settings/{{viewId}}", Name = "viewspublicsettings")]
    public async Task<IActionResult> PublicSettings()
    {
        var response = await Mediator.Send(new GetViewById.Query { Id = CurrentView.Id });

        var webTemplates = await Mediator.Send(new GetWebTemplates.Query
        {
            ThemeId = CurrentOrganization.ActiveThemeId,
            ContentTypeId = CurrentView.ContentTypeId,
            PageSize = int.MaxValue,
        });

        var webTemplateIdResponse = await Mediator.Send(new GetWebTemplateByViewId.Query
        {
            ViewId = CurrentView.Id,
            ThemeId = CurrentOrganization.ActiveThemeId,
        });

        var viewModel = new ViewsPublicSettings_ViewModel
        {
            Id = response.Result.Id,
            RoutePath = response.Result.RoutePath,
            IsPublished = response.Result.IsPublished,
            TemplateId = webTemplateIdResponse.Result.Id,
            AvailableTemplates = webTemplates.Result?.Items.ToDictionary(p => p.Id.ToString(), p => p.Label),
            WebsiteUrl = CurrentOrganization.WebsiteUrl.TrimEnd('/') + CurrentOrganization.PathBase + "/",
            IgnoreClientFilterAndSortQueryParams = response.Result.IgnoreClientFilterAndSortQueryParams,
            MaxNumberOfItemsPerPage = response.Result.MaxNumberOfItemsPerPage,
            DefaultNumberOfItemsPerPage = response.Result.DefaultNumberOfItemsPerPage,
            IsHomePage = CurrentOrganization.HomePageId == CurrentView.Id
        };

        return View(viewModel);
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/public-settings/{{viewId}}", Name = "viewspublicsettings")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PublicSettings(ViewsPublicSettings_ViewModel model)
    {
        var input = new EditPublicSettings.Command
        {
            Id = model.Id,
            RoutePath = model.RoutePath,
            IsPublished = model.IsPublished,
            TemplateId = model.TemplateId,
            IgnoreClientFilterAndSortQueryParams = model.IgnoreClientFilterAndSortQueryParams,
            MaxNumberOfItemsPerPage = model.MaxNumberOfItemsPerPage,
            DefaultNumberOfItemsPerPage = model.DefaultNumberOfItemsPerPage
        };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"Public settings updated successfully.");
            return RedirectToAction("PublicSettings", "Views", new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName, viewId = CurrentView.Id });
        }
        else
        {
            SetErrorMessage("There were validation errors with your form submission. Please correct the fields below.", response.GetErrors());

            var webTemplatesResponse = await Mediator.Send(new GetWebTemplates.Query
            {
                ThemeId = CurrentOrganization.ActiveThemeId,
                ContentTypeId = CurrentView.ContentTypeId,
                PageSize = int.MaxValue
            });

            model.AvailableTemplates = webTemplatesResponse.Result?.Items.ToDictionary(p => p.Id.ToString(), p => p.Label);
            model.WebsiteUrl = CurrentOrganization.WebsiteUrl.TrimEnd('/') + CurrentOrganization.PathBase + "/";
            return View(model);
        }
    }

    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/set-as-home-page/{{viewId}}", Name = "viewssetashomepage")]
    [HttpPost]
    public async Task<IActionResult> SetAsHomePage()
    {
        var input = new SetAsHomePage.Command { Id = CurrentView.Id };
        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage($"Home page updated successfully.");
        }
        else
        {
            SetErrorMessage($"There was an error setting this as the home page.", response.GetErrors());
        }

        return RedirectToAction("PublicSettings", "Views", new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName, viewId = CurrentView.Id });
    }

    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION)]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/{{viewId}}/favorite", Name = "viewsfavorite")]
    [HttpPost]
    public async Task<IActionResult> Favorite()
    {
        var response = await Mediator.Send(new ToggleViewAsFavoriteForAdmin.Command
        {
            UserId = CurrentUser.UserId.Value,
            ViewId = CurrentView.Id,
            SetAsFavorite = true
        });
        if (response.Success)
        {
            SetSuccessMessage($"Successfully favorited view.");
        }
        else
        {
            SetErrorMessage(response.Error);
        }
        return RedirectToAction("Index", "ContentItems", new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName, id = CurrentView.Id });
    }

    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION)]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/{{viewId}}/unfavorite", Name = "viewsunfavorite")]
    [HttpPost]
    public async Task<IActionResult> Unfavorite()
    {
        var response = await Mediator.Send(new ToggleViewAsFavoriteForAdmin.Command
        {
            UserId = CurrentUser.UserId.Value,
            ViewId = CurrentView.Id,
            SetAsFavorite = false
        });
        if (response.Success)
        {
            SetSuccessMessage($"Successfully unfavorited view.");
        }
        else
        {
            SetErrorMessage(response.Error);
        }
        return RedirectToAction("Index", "ContentItems", new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName, id = CurrentView.Id });
    }

    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/{{viewId}}/delete", Name = "viewsdelete")]
    [HttpPost]
    public async Task<IActionResult> Delete()
    {
        var response = await Mediator.Send(new DeleteView.Command { Id = CurrentView.Id });
        if (response.Success)
        {
            SetSuccessMessage($"Successfully deleted view.");
            return RedirectToAction("Index", new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName });
        }
        else
        {
            SetErrorMessage("There was an error deleting this view.", response.GetErrors());
            return RedirectToAction("Index", "ContentItems", new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName, id = CurrentView.Id });
        }
    }

    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/{{viewId}}/columns", Name = "viewscolumns")]
    public async Task<IActionResult> Columns()
    {
        var response = await Mediator.Send(new GetContentTypeFields.Query
        {
            PageSize = int.MaxValue,
            OrderBy = $"FieldOrder {SortOrder.ASCENDING}",
            DeveloperName = CurrentView.ContentType.DeveloperName
        });

        var columnListItems = response.Result.Items.Select(p => new ViewsColumnsListItem_ViewModel
        {
            Label = p.Label,
            DeveloperName = p.DeveloperName,
            Selected = CurrentView.Columns.Contains(p.DeveloperName),
            FieldOrder = CurrentView.Columns.ToList().IndexOf(p.DeveloperName)
        }).ToList();

        var builtInListItems = BuiltInContentTypeField.ReservedContentTypeFields.Select(p => new ViewsColumnsListItem_ViewModel
        {
            Label = p.Label,
            DeveloperName = p.DeveloperName,
            Selected = CurrentView.Columns.Contains(p.DeveloperName),
            FieldOrder = CurrentView.Columns.ToList().IndexOf(p.DeveloperName)
        }).ToList();

        columnListItems.AddRange(builtInListItems);

        var selectedColumns = columnListItems.Where(p => p.Selected).OrderBy(c => c.FieldOrder);
        var notSelectedColumns = columnListItems.Where(p => !p.Selected).OrderBy(c => c.DeveloperName);

        var model = new ViewsColumns_ViewModel
        {
            SelectedColumns = selectedColumns.ToArray(),
            NotSelectedColumns = notSelectedColumns.ToArray()
        };

        return View(model);
    }

    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/{{viewId}}/columns/reorder/{{developerName}}", Name = "viewscolumnsreorderajax")]
    [HttpPatch]
    public async Task<JsonResult> ColumnsReorderAjax(string developerName)
    {
        var position = Request.Form["position"];
        var input = new ReorderColumn.Command
        {
            Id = CurrentView.Id,
            DeveloperName = developerName,
            NewFieldOrder = Convert.ToInt32(position)
        };

        var response = await Mediator.Send(input);
        if (!response.Success)
            HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        return Json(response);
    }

    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/{{viewId}}/columns/toggle/{{developerName}}", Name = "viewscolumnstoggle")]
    [HttpPost]
    public async Task<IActionResult> ColumnsToggle(string developerName)
    {
        var action = Request.Form["action"];
        var command = new EditColumn.Command
        {
            Id = CurrentView.Id,
            DeveloperName = developerName,
            ShowColumn = action == "add"
        };

        var response = await Mediator.Send(command);
        if (!response.Success)
            SetErrorMessage("There was an error adding this column to the view", response.GetErrors());

        return RedirectToAction("Columns", new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName, viewId = CurrentView.Id });
    }

    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/{{viewId}}/sort", Name = "viewssort")]
    public async Task<IActionResult> Sort()
    {
        var response = await Mediator.Send(new GetContentTypeFields.Query
        {
            PageSize = int.MaxValue,
            OrderBy = $"FieldOrder {SortOrder.ASCENDING}",
            DeveloperName = CurrentView.ContentType.DeveloperName
        });

        var columnListItems = response.Result.Items.Where(c => c.FieldType.DeveloperName != BaseFieldType.MultipleSelect).Select(p => new ViewsSortListItem_ViewModel
        {
            Label = p.Label,
            DeveloperName = p.DeveloperName,
            Selected = CurrentView.Sort.Any(c => c.DeveloperName == p.DeveloperName),
            FieldOrder = CurrentView.Sort.Select(p => p.DeveloperName).ToList().IndexOf(p.DeveloperName),
            OrderByDirection = CurrentView.Sort.Any(c => c.DeveloperName == p.DeveloperName) ? CurrentView.Sort.First(c => c.DeveloperName == p.DeveloperName).SortOrder.DeveloperName : SortOrder.ASCENDING
        }).ToList();

        var builtInListItems = BuiltInContentTypeField.ReservedContentTypeFields.Select(p => new ViewsSortListItem_ViewModel
        {
            Label = p.Label,
            DeveloperName = p.DeveloperName,
            Selected = CurrentView.Sort.Any(c => c.DeveloperName == p.DeveloperName),
            FieldOrder = CurrentView.Sort.Select(p => p.DeveloperName).ToList().IndexOf(p.DeveloperName),
            OrderByDirection = CurrentView.Sort.Any(c => c.DeveloperName == p.DeveloperName) ? CurrentView.Sort.First(c => c.DeveloperName == p.DeveloperName).SortOrder.DeveloperName : SortOrder.ASCENDING
        }).ToList();

        columnListItems.AddRange(builtInListItems);

        var selectedColumns = columnListItems.Where(p => p.Selected).OrderBy(c => c.FieldOrder);
        var notSelectedColumns = columnListItems.Where(p => !p.Selected).OrderBy(c => c.DeveloperName);

        var model = new ViewsSort_ViewModel
        {
            SelectedColumns = selectedColumns.ToArray(),
            NotSelectedColumns = notSelectedColumns.ToDictionary(p => p.DeveloperName, p => p.Label)
        };

        return View(model);
    }

    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/{{viewId}}/sort/remove/{{developerName}}", Name = "viewssortremove")]
    [HttpPost]
    public async Task<IActionResult> SortRemove(string developerName)
    {
        var command = new EditSort.Command
        {
            Id = CurrentView.Id,
            DeveloperName = developerName,
            ShowColumn = false
        };

        var response = await Mediator.Send(command);
        if (!response.Success)
            SetErrorMessage("There was an error adding this column to the view", response.GetErrors());

        return RedirectToAction("Sort", new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName, viewId = CurrentView.Id });
    }

    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/{{viewId}}/sort/add", Name = "viewssortadd")]
    [HttpPost]
    public async Task<IActionResult> SortAdd()
    {
        var developerName = Request.Form["DeveloperName"];
        var orderByDirection = Request.Form["OrderByDirection"];
        var command = new EditSort.Command
        {
            Id = CurrentView.Id,
            DeveloperName = developerName,
            ShowColumn = true,
            OrderByDirection = orderByDirection
        };

        var response = await Mediator.Send(command);
        if (!response.Success)
            SetErrorMessage("There was an error adding this column to the view", response.GetErrors());

        return RedirectToAction("Sort", new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName, viewId = CurrentView.Id });
    }

    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/{{viewId}}/sort/reorder/{{developerName}}", Name = "viewssortreorderajax")]
    [HttpPatch]
    public async Task<IActionResult> SortReorderAjax(string developerName)
    {
        var position = Request.Form["position"];
        var input = new ReorderSort.Command
        {
            Id = CurrentView.Id,
            DeveloperName = developerName,
            NewFieldOrder = Convert.ToInt32(position)
        };

        var response = await Mediator.Send(input);
        if (!response.Success)
            HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        return Json(response);
    }

    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/{{viewId}}/filter", Name = "viewsfilter")]
    public async Task<IActionResult> Filter()
    {
        var response = await Mediator.Send(new GetContentTypeFields.Query
        {
            PageSize = int.MaxValue,
            OrderBy = $"FieldOrder {SortOrder.ASCENDING}",
            DeveloperName = CurrentView.ContentType.DeveloperName
        });

        var contentTypeFields = response.Result.Items.Select(p => new ViewsFilterContentTypeField_ViewModel
        {
            Label = p.Label,
            DeveloperName = p.DeveloperName,
            FieldType = p.FieldType,
            Choices = p.Choices?.Select(p => p.DeveloperName)
        }).ToList();

        var builtInListItems = BuiltInContentTypeField.ReservedContentTypeFields
            .Where(p => p.DeveloperName != BuiltInContentTypeField.CreatorUser && p.DeveloperName != BuiltInContentTypeField.LastModifierUser)
            .Select(p => new ViewsFilterContentTypeField_ViewModel
            {
                Label = p.Label,
                DeveloperName = p.DeveloperName,
                FieldType = p.FieldType
            }).ToList();

        contentTypeFields.AddRange(builtInListItems);

        var model = new ViewsFilter_ViewModel
        {
            ContentTypeFields = contentTypeFields.ToArray(),
        };

        return View(model);
    }

    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/{{viewId}}/filter", Name = "viewsfilter")]
    [HttpPost]
    public async Task<JsonResult> Filter([FromForm] string json)
    {
        IEnumerable<FilterConditionInputDto> filterItemsAsObj = JsonSerializer.Deserialize<IEnumerable<FilterConditionInputDto>>(json);

        var response = await Mediator.Send(new EditFilter.Command
        {
            Id = CurrentView.Id,
            Filter = filterItemsAsObj
        });

        if (!response.Success)
            HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        return Json(response);
    }

    protected ViewDto CurrentView
    {
        get
        {
            return HttpContext.Items["CurrentView"] as ViewDto;
        }
    }
}
