using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Raytha.Application.BackgroundTasks.Queries;
using Raytha.Application.Common.Utils;
using Raytha.Application.ContentItems;
using Raytha.Application.ContentItems.Commands;
using Raytha.Application.ContentItems.Queries;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Application.Themes.Queries;
using Raytha.Application.Themes.WebTemplates.Queries;
using Raytha.Application.Views;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Web.Areas.Admin.Views.ContentItems;
using Raytha.Web.Filters;
using Raytha.Web.Utils;

namespace Raytha.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class ContentItemsController : BaseController
{
    private FieldValueConverter _fieldValueConverter;
    protected FieldValueConverter FieldValueConverter => _fieldValueConverter ??= HttpContext.RequestServices.GetRequiredService<FieldValueConverter>();

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [ServiceFilter(typeof(SetPaginationInformationFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}", Name = "contentitemsdefault")]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/{{viewId}}", Name = "contentitemsindex")]
    public async Task<IActionResult> Index(string search = "",
                                           string filter = "",
                                           string orderBy = "",
                                           int pageNumber = 1,
                                           int pageSize = 50)
    {       
        var input = new GetContentItems.Query
        {
            Search = search,
            ViewId = CurrentView.Id,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy,
            Filter = filter
        };
        var response = await Mediator.Send(input);

        var webTemplateContentItemRelationsResponse = await Mediator.Send(new GetWebTemplateContentItemRelationsByContentTypeId.Query
        {
            ThemeId = CurrentOrganization.ActiveThemeId,
            ContentTypeId = CurrentView.ContentTypeId,
        });

        var items = response.Result.Items.Select(p =>
        {
            var templateLabel = webTemplateContentItemRelationsResponse.Result.Where(wtr => wtr.ContentItemId == p.Id).Select(wtr => wtr.WebTemplate.Label).First();
            return new ContentItemsListItem_ViewModel
            {
                Id = p.Id,
                IsHomePage = CurrentOrganization.HomePageId == p.Id,
                FieldValues = FieldValueConverter.MapToListItemValues(p, templateLabel)
            };
        });

        var viewModel = new ContentItemsPagination_ViewModel(items, response.Result.TotalCount);
        return View(viewModel);
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/create", Name = "contentitemscreate")]
    public async Task<IActionResult> Create()
    {
        var webTemplates = await Mediator.Send(new GetWebTemplates.Query
        {
            ThemeId = CurrentOrganization.ActiveThemeId,
            ContentTypeId = CurrentView.ContentTypeId,
            PageSize = int.MaxValue,
        });

        var fieldValues = CurrentView.ContentType.ContentTypeFields.Select(p => new FieldValue_ViewModel
        {
            Label = p.Label,
            DeveloperName = p.DeveloperName,
            AvailableChoices = p.Choices?.Select(b => new FieldValueChoiceItem_ViewModel
            {
                Label = b.Label,
                DeveloperName = b.DeveloperName,
                Disabled = b.Disabled,
                Value = p.FieldType.DeveloperName == BaseFieldType.MultipleSelect ? "false" : b.DeveloperName
            }).ToArray(),
            FieldType = p.FieldType,
            IsRequired = p.IsRequired,
            Description = p.Description,
            RelatedContentTypeId = p.RelatedContentTypeId
        }).ToArray();

        var viewModel = new ContentItemsCreate_ViewModel
        {
            AvailableTemplates = webTemplates.Result?.Items.ToDictionary(p => p.Id, p => p.Label),
            FieldValues = fieldValues,
            AllowedMimeTypes = FileStorageProviderSettings.AllowedMimeTypes,
            MaxFileSize = FileStorageProviderSettings.MaxFileSize,
            UseDirectUploadToCloud = FileStorageProviderSettings.UseDirectUploadToCloud,
            PathBase = CurrentOrganization.PathBase
        };
        return View(viewModel);
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/create", Name = "contentitemscreate")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ContentItemsCreate_ViewModel model)
    {
        var mappedFieldValuesFromModel = MapFromFieldValueModel(model.FieldValues);
        var input = new CreateContentItem.Command
        {
            SaveAsDraft = model.SaveAsDraft,
            TemplateId = model.TemplateId,
            ContentTypeDeveloperName = CurrentView.ContentType.DeveloperName,
            Content = mappedFieldValuesFromModel
        };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{CurrentView.ContentType.LabelSingular} was created successfully.");
            return RedirectToAction("Edit", new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName, id = response.Result });
        }
        else
        {
            SetErrorMessage("There was an error attempting to save this page. See the error below.", response.GetErrors());

            var webTemplates = await Mediator.Send(new GetWebTemplates.Query
            {
                ThemeId = CurrentOrganization.ActiveThemeId,
                ContentTypeId = CurrentView.ContentTypeId,
                PageSize = int.MaxValue
            });

            var fieldValues = CurrentView.ContentType.ContentTypeFields.Select(p => new FieldValue_ViewModel
            {
                Label = p.Label,
                DeveloperName = p.DeveloperName,
                AvailableChoices = p.Choices?.Select(b => new FieldValueChoiceItem_ViewModel
                {
                    Label = b.Label,
                    DeveloperName = b.DeveloperName,
                    Disabled = b.Disabled,
                    Value = p.FieldType.DeveloperName == BaseFieldType.MultipleSelect ? model.FieldValues.First(c => c.DeveloperName.ToDeveloperName() == p.DeveloperName)
                                                    .AvailableChoices.Where(a => a.Value == "true").Select(z => z.DeveloperName.ToDeveloperName()).Contains(b.DeveloperName).ToString() : b.DeveloperName
                }).ToArray(),
                FieldType = p.FieldType,
                IsRequired = p.IsRequired,
                Description = p.Description,
                RelatedContentItemPrimaryField = model.FieldValues.First(c => c.DeveloperName.ToDeveloperName() == p.DeveloperName).RelatedContentItemPrimaryField,
                RelatedContentTypeId = p.RelatedContentTypeId,
                Value = model.FieldValues.First(c => c.DeveloperName.ToDeveloperName() == p.DeveloperName).Value
            }).ToArray();

            var viewModel = new ContentItemsCreate_ViewModel
            {
                AvailableTemplates = webTemplates.Result?.Items.ToDictionary(p => p.Id, p => p.Label),
                FieldValues = fieldValues,
                AllowedMimeTypes = FileStorageProviderSettings.AllowedMimeTypes,
                MaxFileSize = FileStorageProviderSettings.MaxFileSize,
                UseDirectUploadToCloud = FileStorageProviderSettings.UseDirectUploadToCloud,
                PathBase = CurrentOrganization.PathBase
            };

            return View(viewModel);
        }
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/edit/{{id}}", Name = "contentitemsedit")]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/view/{{id}}", Name = "contentitemsview")]
    public async Task<IActionResult> Edit(string id, string backToListUrl = "")
    {
        var response = await Mediator.Send(new GetContentItemById.Query { Id = id });

        var viewModel = new ContentItemsEdit_ViewModel
        {
            BackToListUrl = backToListUrl,
            Id = response.Result.Id,
            IsDraft = response.Result.IsDraft,
            IsPublished = response.Result.IsPublished,
            AllowedMimeTypes = FileStorageProviderSettings.AllowedMimeTypes,
            MaxFileSize = FileStorageProviderSettings.MaxFileSize,
            UseDirectUploadToCloud = FileStorageProviderSettings.UseDirectUploadToCloud,
            PathBase = CurrentOrganization.PathBase,
            FieldValues = CurrentView.ContentType.ContentTypeFields.Select(p => new FieldValue_ViewModel
            {
                Label = p.Label,
                DeveloperName = p.DeveloperName,
                AvailableChoices = p.Choices?.Select(b => new FieldValueChoiceItem_ViewModel
                {
                    Label = b.Label,
                    DeveloperName = b.DeveloperName,
                    Disabled = b.Disabled,
                    Value = FieldValueConverter.MapValueForChoiceField(p.FieldType, response.Result.DraftContent, p, b),
                }).ToArray(),
                FieldType = p.FieldType,
                IsRequired = p.IsRequired,
                Description = p.Description,
                Value = FieldValueConverter.MapValueForField(p.FieldType, response.Result.DraftContent, p),
                RelatedContentItemPrimaryField = FieldValueConverter.MapRelatedContentItemValueForField(p.FieldType, response.Result.DraftContent, p),
                RelatedContentTypeId = p.RelatedContentTypeId
            }
            ).ToArray()
        };

        return View(viewModel);
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/edit/{{id}}", Name = "contentitemsedit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ContentItemsEdit_ViewModel model, string id)
    {
        var input = new EditContentItem.Command
        {
            Id = id,
            SaveAsDraft = model.SaveAsDraft,
            Content = MapFromFieldValueModel(model.FieldValues)
        };
        var editResponse = await Mediator.Send(input);

        if (editResponse.Success)
        {
            SetSuccessMessage($"{CurrentView.ContentType.LabelSingular} was edited successfully.");
            return RedirectToAction("Edit", new { id, contentTypeDeveloperName = CurrentView.ContentType.DeveloperName });
        }
        else
        {
            SetErrorMessage("There was an error attempting to save this page. See the error below.", editResponse.GetErrors());

            var response = await Mediator.Send(new GetContentItemById.Query { Id = id });

            var viewModel = new ContentItemsEdit_ViewModel
            {
                Id = response.Result.Id,
                IsDraft = response.Result.IsDraft,
                IsPublished = response.Result.IsPublished,
                AllowedMimeTypes = FileStorageProviderSettings.AllowedMimeTypes,
                MaxFileSize = FileStorageProviderSettings.MaxFileSize,
                UseDirectUploadToCloud = FileStorageProviderSettings.UseDirectUploadToCloud,
                PathBase = CurrentOrganization.PathBase,
                FieldValues = CurrentView.ContentType.ContentTypeFields.Select(p => new FieldValue_ViewModel
                {
                    Label = p.Label,
                    DeveloperName = p.DeveloperName,
                    AvailableChoices = p.Choices?.Select(b => new FieldValueChoiceItem_ViewModel
                    {
                        Label = b.Label,
                        DeveloperName = b.DeveloperName,
                        Disabled = b.Disabled,
                        Value = p.FieldType.DeveloperName == BaseFieldType.MultipleSelect ? model.FieldValues.First(c => c.DeveloperName.ToDeveloperName() == p.DeveloperName)
                                                    .AvailableChoices.Where(a => a.Value == "true").Select(z => z.DeveloperName.ToDeveloperName()).Contains(b.DeveloperName).ToString() : b.DeveloperName
                    }).ToArray(),
                    FieldType = p.FieldType,
                    IsRequired = p.IsRequired,
                    Description = p.Description,
                    Value = model.FieldValues.First(c => c.DeveloperName.ToDeveloperName() == p.DeveloperName).Value,
                    RelatedContentItemPrimaryField = model.FieldValues.First(c => c.DeveloperName.ToDeveloperName() == p.DeveloperName).RelatedContentItemPrimaryField,
                    RelatedContentTypeId = p.RelatedContentTypeId
                }).ToArray()
            };

            return View(viewModel);
        }
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/relationship/autocomplete", Name = "contentitemsrelationshipautocomplete")]
    public async Task<IActionResult> RelationshipAutocomplete(string relatedContentTypeId, string q = "")
    {
        var viewModel = new List<KeyValuePair<string, string>>();
        var relatedContentType = await Mediator.Send(new GetContentTypeById.Query { Id = relatedContentTypeId });
        var results = await Mediator.Send(new GetContentItems.Query { ContentType = relatedContentType.Result.DeveloperName, Search = q });
        foreach (var item in results.Result.Items)
        {
            viewModel.Add(new KeyValuePair<string, string>(item.Id.ToString(), item.PrimaryField));
        }

        return View("_Autocomplete", viewModel);
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    [ServiceFilter(typeof(SetPaginationInformationFilterAttribute))]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/revisions/{{id}}", Name = "contentitemsrevisions")]
    public async Task<IActionResult> Revisions(string id, string orderBy = $"CreationTime {SortOrder.DESCENDING}", int pageNumber = 1, int pageSize = 50)
    {
        var input = new GetContentItemRevisionsByContentItemId.Query
        {
            Id = id,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy
        };

        var response = await Mediator.Send(input);

        var items = response.Result.Items.Select(p => new ContentItemsRevisionsListItem_ViewModel
        {
            Id = p.Id,
            CreationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(p.CreationTime),
            CreatorUser = p.CreatorUser != null ? p.CreatorUser.FullName : "N/A",
            ContentAsJson = JsonSerializer.Serialize(p.PublishedContent)
        });

        var viewModel = new ContentItemsRevisionsPagination_ViewModel(items, response.Result.TotalCount) { Id = id };
        return View(viewModel);
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    [ServiceFilter(typeof(SetPaginationInformationFilterAttribute))]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/revisions/{{id}}/{{revisionId}}", Name = "contentitemsrevisionsrevert")]
    [HttpPost]
    public async Task<IActionResult> RevisionsRevert(string id, string revisionId)
    {
        var input = new RevertContentItem.Command { Id = revisionId };
        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage($"{CurrentView.ContentType.LabelSingular} has been reverted.");
            return RedirectToAction("Edit", new { id, contentTypeDeveloperName = CurrentView.ContentType.DeveloperName });
        }
        else
        {
            SetErrorMessage("There was an error reverting this content item", response.GetErrors());
            return RedirectToAction("Revisions", new { id, contentTypeDeveloperName = CurrentView.ContentType.DeveloperName });
        }
    }

    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/unpublish/{{id}}", Name = "contentitemsunpublish")]
    [HttpPost]
    public async Task<IActionResult> Unpublish(string id)
    {
        var input = new UnpublishContentItem.Command { Id = id };
        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage($"{CurrentView.ContentType.LabelSingular} has been unpublished.");
        }
        else
        {
            SetErrorMessage("There was an error unpublishing this content item.", response.GetErrors());
        }
        return RedirectToAction("Edit", new { id, contentTypeDeveloperName = CurrentView.ContentType.DeveloperName });
    }

    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/discard-draft/{{id}}", Name = "contentitemsdiscarddraft")]
    [HttpPost]
    public async Task<IActionResult> DiscardDraft(string id)
    {
        var input = new DiscardDraftContentItem.Command { Id = id };
        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage($"{CurrentView.ContentType.LabelSingular} draft has been discarded.");
        }
        else
        {
            SetErrorMessage("There was an error discarding the draft of this content item.", response.GetErrors());
        }
        return RedirectToAction("Edit", new { id, contentTypeDeveloperName = CurrentView.ContentType.DeveloperName });
    }

    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/delete/{{id}}", Name = "contentitemsdelete")]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var input = new DeleteContentItem.Command { Id = id };
        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage($"{CurrentView.ContentType.LabelSingular} has been deleted.");
            return RedirectToAction("Index", new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName, viewId = CurrentView.Id });
        }
        else
        {
            SetErrorMessage($"There was an error deleting this {CurrentView.ContentType.LabelSingular.ToLower()}", response.GetErrors());
            return RedirectToAction("Edit", new { id, contentTypeDeveloperName = CurrentView.ContentType.DeveloperName });
        }
    }

    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/set-as-home-page/{{id}}", Name = "contentitemssetashomepage")]
    [HttpPost]
    public async Task<IActionResult> SetAsHomePage(string id)
    {
        var input = new SetAsHomePage.Command { Id = id };
        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage($"{CurrentView.ContentType.LabelSingular} set as home page successfully.");
            return RedirectToAction("Settings", new { id, contentTypeDeveloperName = CurrentView.ContentType.DeveloperName });
        }
        else
        {
            SetErrorMessage($"There was an error setting this as the home page.", response.GetErrors());
        }
        return RedirectToAction("Settings", new { id, contentTypeDeveloperName = CurrentView.ContentType.DeveloperName });
    }

    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/settings/{{id}}", Name = "contentitemssettings")]
    public async Task<IActionResult> Settings(string id)
    {
        var response = await Mediator.Send(new GetContentItemById.Query { Id = id });

        var webTemplates = await Mediator.Send(new GetWebTemplates.Query
        {
            ThemeId = CurrentOrganization.ActiveThemeId,
            ContentTypeId = CurrentView.ContentTypeId,
            PageSize = int.MaxValue,
        });

        var webTemplateResponse = await Mediator.Send(new GetWebTemplateByContentItemId.Query
        {
            ContentItemId = id,
            ThemeId = CurrentOrganization.ActiveThemeId,
        });

        var viewModel = new ContentItemsSettings_ViewModel
        {
            Id = response.Result.Id,
            TemplateId = webTemplateResponse.Result.Id,
            IsHomePage = CurrentOrganization.HomePageId == response.Result.Id,
            RoutePath = response.Result.RoutePath,
            WebsiteUrl = CurrentOrganization.WebsiteUrl.TrimEnd('/') + CurrentOrganization.PathBase + "/",
            AvailableTemplates = webTemplates.Result?.Items.ToDictionary(p => p.Id.ToString(), p => p.Label)
        };
        return View(viewModel);
    }

    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/settings/{{id}}", Name = "contentitemssettings")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(ContentItemsSettings_ViewModel model, string id)
    {
        var input = new EditContentItemSettings.Command
        {
            Id = id,
            TemplateId = model.TemplateId,
            RoutePath = model.RoutePath
        };

        var editResponse = await Mediator.Send(input);
        if (editResponse.Success)
        {
            SetSuccessMessage($"{CurrentView.ContentType.LabelSingular} was edited successfully.");
            return RedirectToAction("Settings", new { id, contentTypeDeveloperName = CurrentView.ContentType.DeveloperName });
        }
        else
        {
            SetErrorMessage("There was an error attempting to save this page. See the error below.", editResponse.GetErrors());

            var webTemplatesResponse = await Mediator.Send(new GetWebTemplates.Query
            {
                ThemeId = CurrentOrganization.ActiveThemeId,
                ContentTypeId = CurrentView.ContentTypeId,
                PageSize = int.MaxValue,
            });

            model.AvailableTemplates = webTemplatesResponse.Result?.Items.ToDictionary(p => p.Id.ToString(), p => p.Label);
            model.WebsiteUrl = CurrentOrganization.WebsiteUrl.TrimEnd('/') + CurrentOrganization.PathBase + "/";
            return View(model);
        }
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/{{viewId}}/export-to-csv", Name = "contentitemsexporttocsv")]
    public async Task<IActionResult> BeginExportToCsv()
    {
        return View(new ContentItemsExportToCsv_ViewModel());
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/{{viewId}}/export-to-csv", Name = "contentitemsexporttocsv")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BeginExportToCsv(ContentItemsExportToCsv_ViewModel model)
    {
        var input = new BeginExportContentItemsToCsv.Command
        {
            ViewId = CurrentView.Id,
            ExportOnlyColumnsFromView = model.ViewColumnsOnly
        };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"Export in progress.");
            return RedirectToAction("BackgroundTaskStatus", new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName, id = response.Result });
        }
        else
        {
            SetErrorMessage("There was an error attempting to begin this export. See the error below.", response.GetErrors());
            return View(model);
        }
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/background-task/status/{{id}}", Name = "contentitemsbackgroundtaskstatus")]
    public async Task<IActionResult> BackgroundTaskStatus(string id, bool json = false)
    {
        var response = await Mediator.Send(new GetBackgroundTaskById.Query { Id = id });

        return json ? Ok(response.Result) : View(new ContentItemsBackgroundTaskStatus_ViewModel
        {
            PathBase = CurrentOrganization.PathBase
        });
    }

    private dynamic MapFromFieldValueModel(FieldValue_ViewModel[] fieldValues)
    {
        var mappedFieldValues = new Dictionary<string, dynamic>();

        foreach (var fieldValue in fieldValues)
        {
            var contentTypeField = CurrentView.ContentType.ContentTypeFields.First(p => p.DeveloperName == fieldValue.DeveloperName.ToDeveloperName());
            if (contentTypeField.FieldType.DeveloperName == BaseFieldType.OneToOneRelationship)
            {
                Guid guid = (ShortGuid)fieldValue.Value;
                mappedFieldValues.Add(fieldValue.DeveloperName.ToDeveloperName(), guid);
            }
            else if (contentTypeField.FieldType.DeveloperName == BaseFieldType.MultipleSelect)
            {
                var selectedChoices = fieldValue.AvailableChoices.Where(p => p.Value == "true").Select(p => p.DeveloperName).ToArray();
                mappedFieldValues.Add(fieldValue.DeveloperName, contentTypeField.FieldType.FieldValueFrom(selectedChoices).Value);
            }
            else
            {
                mappedFieldValues.Add(fieldValue.DeveloperName, fieldValue.Value);
            }
        }

        return mappedFieldValues;
    }

    protected ViewDto CurrentView
    {
        get
        {
            return HttpContext.Items["CurrentView"] as ViewDto;
        }
    }

    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/{{viewId}}/import-from-csv", Name = "contentitemsimportfromcsv")]
    public async Task<IActionResult> BeginImportFromCsv()
    {
        return View(new ContentItemsImportFromCsv_ViewModel());
    }
    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/{{viewId}}/import-from-csv", Name = "contentitemsimportfromcsv")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BeginImportFromCsv(ContentItemsImportFromCsv_ViewModel model)
    {
       
        IFormFile importFile = model.ImportFile;
        byte[] fileBytes = null;
        if (importFile != null)
        {
            using (var fileStream = importFile.OpenReadStream())
            using (var ms = new MemoryStream())
            {
                fileStream.CopyTo(ms);
                fileBytes = ms.ToArray();
            }
        }

        var input = new BeginImportContentItemsFromCsv.Command
        {
            ImportMethod = model.ImportMethod,
            CsvAsBytes = fileBytes,
            ImportAsDraft = model.ImportAsDraft,
            ContentTypeId = CurrentView.ContentTypeId
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"Import in progress.");
            return RedirectToAction("BackgroundTaskStatus", new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName, id = response.Result });
        }
        else
        {
            SetErrorMessage("There was an error attempting while importing. See the error below.", response.GetErrors());
            return View(model);
        }
    }
}