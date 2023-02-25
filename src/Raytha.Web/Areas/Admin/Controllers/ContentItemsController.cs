using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Utils;
using Raytha.Application.ContentItems;
using Raytha.Application.ContentItems.Commands;
using Raytha.Application.ContentItems.Queries;
using Raytha.Application.ContentTypes;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Application.Templates.Web.Queries;
using Raytha.Application.Views;
using Raytha.Domain.Common;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Domain.ValueObjects.FieldValues;
using Raytha.Web.Areas.Admin.Views.ContentItems;
using Raytha.Web.Filters;
using Raytha.Web.Utils;

namespace Raytha.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class ContentItemsController : BaseController
{
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

        var items = response.Result.Items.Select(p => new ContentItemsListItem_ViewModel
        {
            Id = p.Id,
            IsHomePage = CurrentOrganization.HomePageId == p.Id,
            FieldValues = MapToListItemValues(p)
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
                    Value = MapValueForChoiceField(p.FieldType, response.Result.DraftContent, p, b),
                }).ToArray(),
                FieldType = p.FieldType,
                IsRequired = p.IsRequired,
                Description = p.Description,
                Value = MapValueForField(p.FieldType, response.Result.DraftContent, p),
                RelatedContentItemPrimaryField = MapRelatedContentItemValueForField(p.FieldType, response.Result.DraftContent, p),
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
            ContentTypeId = CurrentView.ContentTypeId,
            PageSize = int.MaxValue
        });

        var viewModel = new ContentItemsSettings_ViewModel
        {
            Id = response.Result.Id,
            TemplateId = response.Result.WebTemplate.Id,
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

            var response = await Mediator.Send(new GetContentItemById.Query { Id = id });

            var webTemplates = await Mediator.Send(new GetWebTemplates.Query { ContentTypeId = CurrentView.ContentTypeId, PageSize = int.MaxValue });

            model.AvailableTemplates = webTemplates.Result?.Items.ToDictionary(p => p.Id.ToString(), p => p.Label);
            model.WebsiteUrl = CurrentOrganization.WebsiteUrl.TrimEnd('/') + CurrentOrganization.PathBase + "/";
            return View(model);
        }
    }

    private Dictionary<string, string> MapToListItemValues(ContentItemDto item)
    {
        var viewModel = new Dictionary<string, string>
        {
            //Built in
            { BuiltInContentTypeField.Id, item.Id },
            { "CreatorUser", item.CreatorUser != null ? item.CreatorUser.FullName : "N/A" },
            { "LastModifierUser", item.LastModifierUser != null ? item.LastModifierUser.FullName : "N/A" },
            { BuiltInContentTypeField.CreationTime, CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(item.CreationTime) },
            { BuiltInContentTypeField.LastModificationTime, CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(item.LastModificationTime) },
            { BuiltInContentTypeField.IsPublished, item.IsPublished.YesOrNo() },
            { BuiltInContentTypeField.IsDraft, item.IsDraft.YesOrNo() },
            { BuiltInContentTypeField.PrimaryField, item.PrimaryField },
            { "Template", item.WebTemplate.Label }
        };

        //Content type fields
        foreach (var field in item.PublishedContent as Dictionary<string, dynamic>)
        {
            if (field.Value is DateTimeFieldValue dateTimeFieldValue)
            {
                viewModel.Add(field.Key, CurrentOrganization.TimeZoneConverter.ToDateFormat(dateTimeFieldValue.Value));
            }
            else if (field.Value is GuidFieldValue guidFieldValue)
            {
                if (guidFieldValue.HasValue)
                    viewModel.Add(field.Key, (ShortGuid)guidFieldValue.Value);
                else
                    viewModel.Add(field.Key, string.Empty);
            }
            else if (field.Value is StringFieldValue)
            {
                viewModel.Add(field.Key, ((string)field.Value).StripHtml().Truncate(40));
            }
            else if (field.Value is IBaseEntity)
            {
                viewModel.Add(field.Key, field.Value.PrimaryField);
            }
            else
            {
                viewModel.Add(field.Key, field.Value.ToString());
            }
        }

        return viewModel;
    }

    private dynamic MapFromFieldValueModel(FieldValue_ViewModel[] fieldValues)
    {
        var mappedFieldValues = new Dictionary<string, dynamic>();

        foreach (var fieldValue in fieldValues)
        {
            var contentTypeField = CurrentView.ContentType.ContentTypeFields.First(p => p.DeveloperName == fieldValue.DeveloperName.ToDeveloperName());

            if (contentTypeField.FieldType.DeveloperName == BaseFieldType.MultipleSelect)
            {
                var selectedChoices = fieldValue.AvailableChoices.Where(p => p.Value == "true").Select(p => p.DeveloperName).ToArray();
                mappedFieldValues.Add(fieldValue.DeveloperName, contentTypeField.FieldType.FieldValueFrom(selectedChoices).Value);
            }
            else if (contentTypeField.FieldType.DeveloperName == BaseFieldType.OneToOneRelationship)
            {
                Guid guid = (ShortGuid)fieldValue.Value;
                mappedFieldValues.Add(fieldValue.DeveloperName.ToDeveloperName(), contentTypeField.FieldType.FieldValueFrom(guid).Value);
            }
            else
            {
                mappedFieldValues.Add(fieldValue.DeveloperName.ToDeveloperName(), contentTypeField.FieldType.FieldValueFrom(fieldValue.Value).Value);
            }
        }

        return mappedFieldValues;
    }

    private string MapValueForChoiceField(BaseFieldType fieldType, dynamic content, ContentTypeFieldDto contentTypeField, ContentTypeFieldChoice contentTypeFieldChoice)
    {
        string value = "false";
        if (fieldType.DeveloperName == BaseFieldType.MultipleSelect)
        {
            if (content.ContainsKey(contentTypeField.DeveloperName) && content[contentTypeField.DeveloperName].HasValue)
            {
                var asArray = content[contentTypeField.DeveloperName].Value as IList<string>;
                bool tempValue = asArray.Contains(contentTypeFieldChoice.DeveloperName);
                value = tempValue.ToString();
            }
        }
        else
        {
            value = contentTypeFieldChoice.DeveloperName;
        }
        return value;
    }

    private string MapValueForField(BaseFieldType fieldType, dynamic content, ContentTypeFieldDto contentTypeField)
    {
        string value = string.Empty;
        if (fieldType.DeveloperName == BaseFieldType.OneToOneRelationship)
        {
            ShortGuid shortGuid = ShortGuid.Empty;
            if (content.ContainsKey(contentTypeField.DeveloperName) && content[contentTypeField.DeveloperName] != null)
            {
                var successfullyParsed = ShortGuid.TryParse(content[contentTypeField.DeveloperName].ToString(), out shortGuid) || (content[contentTypeField.DeveloperName] is ContentItemDto && ShortGuid.TryParse(content[contentTypeField.DeveloperName].Id.ToString(), out shortGuid));
                if (successfullyParsed)
                {
                    value = shortGuid;
                }
            }
        }
        else if (fieldType.DeveloperName == BaseFieldType.Checkbox)
        {
            if (content.ContainsKey(contentTypeField.DeveloperName))
            {
                bool tempValue = content[contentTypeField.DeveloperName].HasValue && content[contentTypeField.DeveloperName].Value;
                value = tempValue.ToString();
            }
            else
            {
                value = "false";
            }
        }
        else if (content.ContainsKey(contentTypeField.DeveloperName))
        {
            value = content[contentTypeField.DeveloperName].ToString();
        }
        return value;
    }

    private string MapRelatedContentItemValueForField(BaseFieldType fieldType, dynamic content, ContentTypeFieldDto contentTypeField)
    {
        string value = string.Empty;
        if (fieldType.DeveloperName == BaseFieldType.OneToOneRelationship)
        {
            if (content.ContainsKey(contentTypeField.DeveloperName) && content[contentTypeField.DeveloperName] is ContentItemDto && content[contentTypeField.DeveloperName] != null)
            {
                value = content[contentTypeField.DeveloperName].PrimaryField;
            }
        }
        return value;
    }

    protected ViewDto CurrentView
    {
        get
        {
            return HttpContext.Items["CurrentView"] as ViewDto;
        }
    }
}