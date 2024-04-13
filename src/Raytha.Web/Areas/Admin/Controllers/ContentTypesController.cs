using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using System.Linq;
using Raytha.Application.ContentTypes.Commands;
using Raytha.Domain.ValueObjects;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Web.Filters;
using Microsoft.AspNetCore.Http;
using Raytha.Application.Views;
using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Application.ContentTypes;
using Raytha.Web.Areas.Admin.Views.ContentTypes;
using Raytha.Application.Common.Utils;
using Raytha.Application.ContentItems.Queries;
using Raytha.Application.ContentItems.Commands;
using Microsoft.AspNetCore.Authorization;
using Raytha.Domain.Entities;
using Raytha.Web.Utils;

namespace Raytha.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class ContentTypesController : BaseController
{
    [Authorize(Policy = BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION)]
    [Route($"{RAYTHA_ROUTE_PREFIX}/content-types/create", Name = "contenttypescreate")]
    public async Task<IActionResult> Create()
    {
        var viewModel = new ContentTypesCreate_ViewModel
        {
            WebsiteUrl = CurrentOrganization.WebsiteUrl.TrimEnd('/') + "/",
            DefaultRouteTemplate = "{ContentTypeDeveloperName}/{PrimaryField}"
        };
        return View(viewModel);
    }

    [Authorize(Policy = BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION)]
    [Route($"{RAYTHA_ROUTE_PREFIX}/content-types/create", Name = "contenttypescreate")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ContentTypesCreate_ViewModel model)
    {
        var input = new CreateContentType.Command
        {
            LabelPlural = model.LabelPlural,
            LabelSingular = model.LabelSingular,
            DefaultRouteTemplate = model.DefaultRouteTemplate,
            Description = model.Description,
            DeveloperName = model.DeveloperName
        };

        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage($"{model.LabelPlural} edit successfully.");
            return RedirectToAction("Index", "ContentItems", new { contentTypeDeveloperName = model.DeveloperName.ToDeveloperName() });
        }
        else
        {
            SetErrorMessage("There were validation errors with your form submission. Please correct the fields below.", response.GetErrors());
            model.WebsiteUrl = CurrentOrganization.WebsiteUrl.TrimEnd('/') + "/";
            return View(model);
        }
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/configuration", Name = "contenttypesconfiguration")]
    public async Task<IActionResult> Configuration()
    {
        var viewModel = new ContentTypesEdit_ViewModel
        {
            LabelPlural = CurrentView.ContentType.LabelPlural,
            LabelSingular = CurrentView.ContentType.LabelSingular,
            DeveloperName = CurrentView.ContentType.DeveloperName,
            DefaultRouteTemplate = CurrentView.ContentType.DefaultRouteTemplate,
            WebsiteUrl = CurrentOrganization.WebsiteUrl.TrimEnd('/') + "/",
            Description = CurrentView.ContentType.Description,
            Id = CurrentView.ContentTypeId,
            PrimaryFieldId = CurrentView.ContentType.PrimaryFieldId,
            ContentTypeFields = CurrentView.ContentType.ContentTypeFields.Where(p => p.FieldType.DeveloperName == BaseFieldType.SingleLineText).ToDictionary(p => p.Id.ToString(), p => p.DeveloperName)
        };

        return View(viewModel);
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/configuration", Name = "contenttypesconfiguration")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Configuration(ContentTypesEdit_ViewModel model)
    {
        var input = new EditContentType.Command
        {
            LabelPlural = model.LabelPlural,
            LabelSingular = model.LabelSingular,
            DefaultRouteTemplate = model.DefaultRouteTemplate,
            Id = CurrentView.ContentTypeId,
            Description = model.Description,
            PrimaryFieldId = model.PrimaryFieldId
        };

        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage($"{model.LabelPlural} edit successfully.");
            return RedirectToAction("Configuration");
        }
        else
        {
            SetErrorMessage("There were validation errors with your form submission. Please correct the fields below.", response.GetErrors());
            model.ContentTypeFields = CurrentView.ContentType.ContentTypeFields.Where(p => p.FieldType.DeveloperName == BaseFieldType.SingleLineText).ToDictionary(p => p.Id.ToString(), p => p.DeveloperName);
            model.WebsiteUrl = CurrentOrganization.WebsiteUrl.TrimEnd('/') + "/";
            return View(model);
        }
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [ServiceFilter(typeof(SetPaginationInformationFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/fields", Name = "contenttypesfieldslist")]
    public async Task<IActionResult> FieldsList(string search = "", string orderBy = $"FieldOrder {SortOrder.ASCENDING}", int pageNumber = 1, int pageSize = 50)
    {
        var input = new GetContentTypeFields.Query
        {
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy,
            DeveloperName = CurrentView.ContentType.DeveloperName,
            ShowDeletedOnly = false
        };
        var response = await Mediator.Send(input);

        var items = response.Result.Items.Select(p => new FieldsListItem_ViewModel
        {
            Id = p.Id,
            Label = p.Label,
            DeveloperName = p.DeveloperName,
            FieldType = p.FieldType,
            IsRequired = p.IsRequired.YesOrNo()
        });

        var viewModel = new FieldsPagination_ViewModel(items, response.Result.TotalCount, false);

        return View(viewModel);
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [ServiceFilter(typeof(SetPaginationInformationFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/deleted-fields", Name = "contenttypesfieldslistdeleted")]
    public async Task<IActionResult> FieldsListDeleted(string search = "", string orderBy = $"FieldOrder {SortOrder.ASCENDING}", int pageNumber = 1, int pageSize = 50)
    {
        var input = new GetContentTypeFields.Query
        {
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy,
            DeveloperName = CurrentView.ContentType.DeveloperName,
            ShowDeletedOnly = true
        };
        var response = await Mediator.Send(input);

        var items = response.Result.Items.Select(p => new FieldsListItem_ViewModel
        {
            Id = p.Id,
            Label = p.Label,
            DeveloperName = p.DeveloperName,
            FieldType = p.FieldType,
            IsRequired = p.IsRequired.YesOrNo()
        });

        var viewModel = new FieldsPagination_ViewModel(items, response.Result.TotalCount, true);

        return View("FieldsList", viewModel);
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/fields/create", Name = "contenttypesfieldscreate")]
    public async Task<IActionResult> FieldsCreate()
    {
        var contentTypes = await Mediator.Send(new GetContentTypes.Query());

        var viewModel = new FieldsCreate_ViewModel
        {
            ContentTypeId = CurrentView.ContentTypeId,
            AvailableContentTypes = contentTypes.Result.Items.ToDictionary(p => p.Id.ToString(), p => p.LabelPlural),
            AvailableFieldTypes = BaseFieldType.SupportedTypes.ToDictionary(p => p.DeveloperName, p => p.Label)
        };

        return View(viewModel);
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/fields/create", Name = "contenttypesfieldscreate")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FieldsCreate(FieldsCreate_ViewModel model)
    {
        var input = new CreateContentTypeField.Command
        {
            Label = model.Label,
            DeveloperName = model.DeveloperName,
            ContentTypeId = model.ContentTypeId,
            FieldType = model.FieldType,
            Choices = model.Choices.Select(p => new ContentTypeFieldChoiceInputDto { Label = p.Label, DeveloperName = p.DeveloperName, Disabled = p.Disabled }),
            IsRequired = model.IsRequired,
            Description = model.Description,
            RelatedContentTypeId = model.RelatedContentTypeId
        };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{model.Label} was created successfully.");
            return RedirectToAction("FieldsList", new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName });
        }
        else
        {
            var contentTypes = await Mediator.Send(new GetContentTypes.Query());
            SetErrorMessage("There was an error attempting to save this field definition. See the error below.", response.GetErrors());

            model.AvailableContentTypes = contentTypes.Result.Items.ToDictionary(p => p.Id.ToString(), p => p.LabelPlural);
            model.AvailableFieldTypes = BaseFieldType.SupportedTypes.ToDictionary(p => p.DeveloperName, p => p.Label);
            return View(model);
        }
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/fields/edit/{{id}}", Name = "contenttypesfieldsedit")]
    public async Task<IActionResult> FieldsEdit(string id)
    {
        var response = await Mediator.Send(new GetContentTypeFieldById.Query { Id = id });
        var contentTypes = await Mediator.Send(new GetContentTypes.Query());
        
        var viewModel = new EditField_ViewModel
        {
            Id = response.Result.Id,
            Label = response.Result.Label,
            FieldType = response.Result.FieldType,
            DeveloperName = response.Result.DeveloperName,
            Choices = response.Result.Choices?.Select(p => new FieldChoiceItem_ViewModel { Label = p.Label, DeveloperName = p.DeveloperName, Disabled = p.Disabled }).ToArray(),
            IsRequired = response.Result.IsRequired,
            Description = response.Result.Description,
            AvailableContentTypes = contentTypes.Result.Items.ToDictionary(p => p.Id.ToString(), p => p.LabelPlural),
            AvailableFieldTypes = BaseFieldType.SupportedTypes.ToDictionary(p => p.DeveloperName, p => p.Label),
            RelatedContentTypeId = response.Result.RelatedContentTypeId
        };

        return View(viewModel);
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/fields/edit/{{id}}", Name = "contenttypesfieldsedit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FieldsEdit(EditField_ViewModel model, string id)
    {
        var input = new EditContentTypeField.Command
        {
            Id = id,
            Label = model.Label,
            Choices = model.Choices?.Select(p => new ContentTypeFieldChoiceInputDto { Label = p.Label, DeveloperName = p.DeveloperName, Disabled = p.Disabled }),
            IsRequired = model.IsRequired,
            Description = model.Description
        };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{model.Label} was edited successfully.");
            return RedirectToAction("FieldsList", new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName });
        }
        else
        {
            SetErrorMessage("There was an error attempting to save this field definition. See the error below.", response.GetErrors());

            var contentTypes = await Mediator.Send(new GetContentTypes.Query());
            model.AvailableContentTypes = contentTypes.Result.Items.ToDictionary(p => p.Id.ToString(), p => p.LabelPlural);
            model.AvailableFieldTypes = BaseFieldType.SupportedTypes.ToDictionary(p => p.DeveloperName, p => p.Label);
            return View(model);
        }
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [HttpPost]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/fields/delete/{{id}}", Name = "contenttypesfieldsdelete")]
    public async Task<IActionResult> FieldsDelete(string id)
    {
        var input = new DeleteContentTypeField.Command { Id = id };
        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage($"Content type field has been deleted.");
            return RedirectToAction("FieldsList", new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName });
        }
        else
        {
            SetErrorMessage("There was an error deleting this content type field", response.GetErrors());
            return RedirectToAction("FieldsEdit", new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName, id });
        }
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/fields/reorder", Name = "contenttypesfieldsreorder")]
    public async Task<IActionResult> FieldsReorder()
    {
        var input = new GetContentTypeFields.Query
        {
            PageSize = int.MaxValue,
            OrderBy = $"FieldOrder {SortOrder.ASCENDING}",
            DeveloperName = CurrentView.ContentType.DeveloperName
        };

        var response = await Mediator.Send(input);

        var items = response.Result.Items.Select(p => new FieldsListItem_ViewModel
        {
            Id = p.Id,
            Label = p.Label,
            DeveloperName = p.DeveloperName,
            FieldType = p.FieldType,
            IsRequired = p.IsRequired.YesOrNo()
        });

        var viewModel = new FieldsPagination_ViewModel(items, response.Result.TotalCount, false);

        return View(viewModel);
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/fields/reorder/{{id}}", Name = "contenttypesfieldsreorderajax")]
    [HttpPatch]
    public async Task<IActionResult> FieldsReorderAjax(string id)
    {
        var position = Request.Form["position"];
        var input = new ReorderContentTypeField.Command
        {
            Id = id,
            NewFieldOrder = Convert.ToInt32(position)
        };

        var result = await Mediator.Send(input);
        if (result.Success)
            return Ok(result);
        else
            return BadRequest(result);
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [ServiceFilter(typeof(SetPaginationInformationFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/trash", Name = "contenttypesdeletedcontentitemslist")]
    public async Task<IActionResult> DeletedContentItemsList(string search = "", string orderBy = $"CreationTime {SortOrder.DESCENDING}", int pageNumber = 1, int pageSize = 50)
    {
        var input = new GetDeletedContentItems.Query
        {
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy,
            DeveloperName = CurrentView.ContentType.DeveloperName
        };
        var response = await Mediator.Send(input);

        var items = response.Result.Items.Select(p => new DeletedContentItemsListItem_ViewModel
        {
            Id = p.Id,
            PrimaryField = p.PrimaryField,
            DeletedBy = p.CreatorUser?.FullName ?? "N/A",
            DeletionTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(p.CreationTime),
            OriginalContentItemId = p.OriginalContentItemId
        });

        var viewModel = new DeletedContentItemsPagination_ViewModel(items, response.Result.TotalCount);

        return View(viewModel);
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/trash/restore/{{id}}", Name = "contenttypesdeletedcontentitemsrestore")]
    [HttpPost]
    public async Task<IActionResult> DeletedContentItemsRestore(string id)
    {
        var input = new RestoreContentItem.Command { Id = id };
        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage($"{CurrentView.ContentType.LabelSingular} has been restored.");
            return RedirectToAction("Edit", "ContentItems", new { id = response.Result.ToString(), contentTypeDeveloperName = CurrentView.ContentType.DeveloperName });
        }
        else
        {
            SetErrorMessage($"There was an error restoring this {CurrentView.ContentType.LabelSingular.ToLower()}", response.GetErrors());
            return RedirectToAction("DeletedContentItemsList", new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName });
        }
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/trash/clear/{{id}}", Name = "contenttypesdeletedcontentitemsclear")]
    [HttpPost]
    public async Task<IActionResult> DeletedContentItemsClear(string id)
    {
        var input = new DeleteAlreadyDeletedContentItem.Command { Id = id };
        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage($"{CurrentView.ContentType.LabelSingular} has been permanently removed.");
        }
        else
        {
            SetErrorMessage($"There was an error permanently removing this {CurrentView.ContentType.LabelSingular.ToLower()}", response.GetErrors());
        }
        return RedirectToAction("DeletedContentItemsList", new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName });
    }

    protected ViewDto CurrentView
    {
        get
        {
            return HttpContext.Items["CurrentView"] as ViewDto;
        }
    }
}