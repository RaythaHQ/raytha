using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.VisualBasic.FileIO;
using Raytha.Application.Common.Utils;
using Raytha.Application.ContentTypes;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Application.Templates.Web;
using Raytha.Application.Templates.Web.Commands;
using Raytha.Application.Templates.Web.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Web.Areas.Admin.Views.Shared.ViewModels;
using Raytha.Web.Areas.Admin.Views.Templates.Web;
using Raytha.Web.Filters;
using Raytha.Web.Utils;

namespace Raytha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION)]
public class WebTemplatesController : BaseController
{
    [ServiceFilter(typeof(SetPaginationInformationFilterAttribute))]
    [Route(RAYTHA_ROUTE_PREFIX + "/templates/web", Name = "webtemplatesindex")]
    public async Task<IActionResult> Index(string search = "", string orderBy = $"Label {SortOrder.ASCENDING}", int pageNumber = 1, int pageSize = 50)
    {
        var input = new GetWebTemplates.Query
        {
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy
        };

        var response = await Mediator.Send(input);

        var items = response.Result.Items.Select(p => new WebTemplatesListItem_ViewModel
        {
            Id = p.Id,
            Label = p.Label,
            DeveloperName = p.DeveloperName,
            LastModificationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(p.LastModificationTime),
            LastModifierUser = p.LastModifierUser != null ? p.LastModifierUser.FullName : "N/A",
            IsBuiltInTemplate = p.IsBuiltInTemplate.YesOrNo(),
            ParentTemplate = p.ParentTemplate != null ? new WebTemplatesListItem_ViewModel.ParentTemplate_ViewModel { Id = p.ParentTemplate.Id, Label = p.ParentTemplate.Label } : null
        });

        var viewModel = new List_ViewModel<WebTemplatesListItem_ViewModel>(items, response.Result.TotalCount);
        
        return View("~/Areas/Admin/Views/Templates/Web/Index.cshtml", viewModel);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/templates/web/create", Name = "webtemplatescreate")]
    public async Task<IActionResult> Create()
    {
        var baseLayouts = await Mediator.Send(new GetWebTemplates.Query
        {
            PageSize = int.MaxValue,
            BaseLayoutsOnly = true
        });        
        var baseLayoutsDictionary = baseLayouts.Result.Items.ToDictionary(k => k.Id.ToString(), v => v.Label);
        
        var contentTypes = await Mediator.Send(new GetContentTypes.Query { PageSize = int.MaxValue });
        var templateAccessList = contentTypes.Result.Items.Select(p => new TemplateAccessToModelDefinitions_ViewModel
        {
            Id = p.Id,
            Key = p.LabelPlural,
            Value = true
        });

        var templateVariableDictionary = GetInsertVariablesViewModel(string.Empty, false, contentTypes.Result.Items);

        var viewModel = new WebTemplatesCreate_ViewModel
        {
            ParentTemplates = baseLayoutsDictionary,
            TemplateAccessToModelDefinitions = templateAccessList.ToArray(),
            TemplateVariables = templateVariableDictionary
        };

        return View("~/Areas/Admin/Views/Templates/Web/Create.cshtml", viewModel);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/templates/web/create", Name = "webtemplatescreate")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WebTemplatesCreate_ViewModel model)
    {
        var input = new CreateWebTemplate.Command
        {
            Label = model.Label,
            Content = model.Content,
            ParentTemplateId = model.ParentTemplateId,
            IsBaseLayout = model.IsBaseLayout,
            DeveloperName = model.DeveloperName,
            TemplateAccessToModelDefinitions = model.TemplateAccessToModelDefinitions.Where(p => p.Value).Select(p => (ShortGuid)p.Id),
            AllowAccessForNewContentTypes = model.AllowAccessForNewContentTypes
        };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{model.Label} was created successfully.");
            return RedirectToAction("Index");
        }
        else
        {
            var baseLayouts = await Mediator.Send(new GetWebTemplates.Query
            {
                PageSize = int.MaxValue,
                BaseLayoutsOnly = true
            });
            var baseLayoutsDictionary = baseLayouts.Result.Items.ToDictionary(k => k.Id.ToString(), v => v.Label);
            model.ParentTemplates = baseLayoutsDictionary;

            var contentTypes = await Mediator.Send(new GetContentTypes.Query { PageSize = int.MaxValue });
            var templateVariableDictionary = GetInsertVariablesViewModel(model.DeveloperName, false, contentTypes.Result.Items);
            model.TemplateVariables = templateVariableDictionary;

            SetErrorMessage("There was an error attempting to update this template. See the error below.", response.GetErrors());
            return View("~/Areas/Admin/Views/Templates/Web/Create.cshtml", model);
        }
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/templates/web/edit/{id}", Name = "webtemplatesedit")]
    public async Task<IActionResult> Edit(string id)
    {
        var response = await Mediator.Send(new GetWebTemplateById.Query { Id = id });

        var baseLayouts = await Mediator.Send(new GetWebTemplates.Query
        {
            PageSize = int.MaxValue,
            BaseLayoutsOnly = true
        });
        var childLayouts = GetChildren(baseLayouts.Result.Items.ToArray(), response.Result);
        var lineage = childLayouts.Union(new List<WebTemplateDto>() { response.Result });
        var excepted = baseLayouts.Result.Items.Select(p => p.DeveloperName).Except(lineage.Select(p => p.DeveloperName));
        var baseLayoutsDictionary = baseLayouts.Result.Items.Where(p => excepted.Contains(p.DeveloperName)).ToDictionary(k => k.Id.ToString(), v => v.Label);

        var contentTypes = await Mediator.Send(new GetContentTypes.Query());
        var templateAccessChoiceItems = new List<TemplateAccessToModelDefinitions_ViewModel>();
        foreach (var contentType in contentTypes.Result.Items)
        {
            templateAccessChoiceItems.Add(new TemplateAccessToModelDefinitions_ViewModel
            {
                Id = contentType.Id,
                Key = contentType.LabelPlural,
                Value = response.Result.TemplateAccessToModelDefinitions.ContainsKey(contentType.Id)
            });
        }

        var templateVariableDictionary = GetInsertVariablesViewModel(response.Result.DeveloperName, response.Result.IsBuiltInTemplate, contentTypes.Result.Items);

        var model = new WebTemplatesEdit_ViewModel
        {
            Id = response.Result.Id,
            Content = response.Result.Content,
            Label = response.Result.Label,
            DeveloperName = response.Result.DeveloperName,
            ParentTemplateId = response.Result.ParentTemplateId,
            ParentTemplates = baseLayoutsDictionary,
            IsBaseLayout = response.Result.IsBaseLayout,
            IsBuiltInTemplate = response.Result.IsBuiltInTemplate,
            TemplateAccessToModelDefinitions = templateAccessChoiceItems.ToArray(),
            TemplateVariables = templateVariableDictionary,
            AllowAccessForNewContentTypes = response.Result.AllowAccessForNewContentTypes
        };

        if (WebTemplateExtensions.HasRenderBodyTag(response.Result.Content) && !response.Result.IsBaseLayout)
            SetWarningMessage("{% renderbody %} is present and this template is not a base layout. This may result in a rendering error or crash if not handled properly.");
           
        return View("~/Areas/Admin/Views/Templates/Web/Edit.cshtml", model);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/templates/web/edit/{id}", Name = "webtemplatesedit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(WebTemplatesEdit_ViewModel model, string id)
    {
        var input = new EditWebTemplate.Command
        {
            Id = id,
            Label = model.Label,
            Content = model.Content,
            ParentTemplateId = model.ParentTemplateId,
            IsBaseLayout = model.IsBaseLayout,
            AllowAccessForNewContentTypes = model.AllowAccessForNewContentTypes,
            TemplateAccessToModelDefinitions = model.TemplateAccessToModelDefinitions.Where(p => p.Value).Select(p => (ShortGuid)p.Id)
        };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{model.Label} was updated successfully.");
            return RedirectToAction("Edit", new { id });
        }
        else 
        {
            var templateResponse = await Mediator.Send(new GetWebTemplateById.Query { Id = id });

            var baseLayouts = await Mediator.Send(new GetWebTemplates.Query
            {
                PageSize = int.MaxValue,
                BaseLayoutsOnly = true
            });

            var childLayouts = GetChildren(baseLayouts.Result.Items.ToArray(), templateResponse.Result);
            var lineage = childLayouts.Union(new List<WebTemplateDto>() { templateResponse.Result });
            var excepted = baseLayouts.Result.Items.Select(p => p.DeveloperName).Except(lineage.Select(p => p.DeveloperName));
            var baseLayoutsDictionary = baseLayouts.Result.Items.Where(p => excepted.Contains(p.DeveloperName)).ToDictionary(k => k.Id.ToString(), v => v.Label);
            model.ParentTemplates = baseLayoutsDictionary;

            var contentTypes = await Mediator.Send(new GetContentTypes.Query { PageSize = int.MaxValue });
            var templateVariableDictionary = GetInsertVariablesViewModel(templateResponse.Result.DeveloperName, templateResponse.Result.IsBuiltInTemplate, contentTypes.Result.Items);
            model.TemplateVariables = templateVariableDictionary;

            SetErrorMessage("There was an error attempting to update this template. See the error below.", response.GetErrors());
            return View("~/Areas/Admin/Views/Templates/Web/Edit.cshtml", model);
        }
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/templates/web/delete/{id}", Name = "webtemplatesdelete")]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var input = new DeleteWebTemplate.Command { Id = id };
        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage($"Template has been deleted.");
            return RedirectToAction("Index");
        }
        else
        {
            SetErrorMessage("There was an error deleting this template", response.GetErrors());
            return RedirectToAction("Edit", new { id });
        }
    }

    [ServiceFilter(typeof(SetPaginationInformationFilterAttribute))]
    [Route(RAYTHA_ROUTE_PREFIX + "/templates/web/edit/{id}/revisions", Name = "webtemplatesrevisionsindex")]
    public async Task<IActionResult> Revisions(string id, string orderBy = $"CreationTime {SortOrder.DESCENDING}", int pageNumber = 1, int pageSize = 50)
    {
        var template = await Mediator.Send(new GetWebTemplateById.Query { Id = id });

        var input = new GetWebTemplateRevisionsByTemplateId.Query
        {
            Id = id,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy,
        };

        var response = await Mediator.Send(input);
        var items = response.Result.Items.Select(p => new WebTemplatesRevisionsListItem_ViewModel
        {
            Id = p.Id,
            Label = p.Label,
            CreationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(p.CreationTime),
            CreatorUser = p.CreatorUser != null ? p.CreatorUser.FullName : "N/A",
            Content = p.Content
        });

        var viewModel = new WebTemplatesRevisionsPagination_ViewModel(items, response.Result.TotalCount)
        {
            TemplateId = template.Result.Id,
            IsBuiltInTemplate = template.Result.IsBuiltInTemplate
        };

        return View("~/Areas/Admin/Views/Templates/Web/Revisions.cshtml", viewModel);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/templates/web/edit/{id}/revisions/{revisionId}", Name = "webtemplatesrevisionsrevert")]
    [HttpPost]
    public async Task<IActionResult> RevisionsRevert(string id, string revisionId)
    {
        var input = new RevertWebTemplate.Command { Id = revisionId };
        var response = await Mediator.Send(input);
        if (response.Success)
            SetSuccessMessage($"Template has been reverted.");
        else
            SetErrorMessage("There was an error reverting this template", response.GetErrors());
        return RedirectToAction("Edit", new { id });
    }

    protected List<WebTemplateDto> GetChildren(WebTemplateDto[] list, WebTemplateDto startItem)
    {
        var result = new List<WebTemplateDto>();
        var children = list.Where(p => p.ParentTemplateId == startItem.Id).ToList();
        foreach (var child in children)
        {
            result.Add(child);
            result.AddRange(GetChildren(list, child));
        }
        return result;
    }

    protected Dictionary<string, IEnumerable<InsertVariableListItem_ViewModel>> GetInsertVariablesViewModel(string templateName, bool isBuiltInTemplate, IEnumerable<ContentTypeDto> contentTypes)
    {
        var templateVariableDictionary = new Dictionary<string, IEnumerable<InsertVariableListItem_ViewModel>>();

        var currentOrgVariables = InsertVariableTemplateFactory.CurrentOrganization.TemplateInfo.GetTemplateVariables().Select(p => new InsertVariableListItem_ViewModel
        {
            DeveloperName = p.Key,
            TemplateVariable = p.Value
        });

        var currentUserVariables = InsertVariableTemplateFactory.CurrentUser.TemplateInfo.GetTemplateVariables().Select(p => new InsertVariableListItem_ViewModel
        {
            DeveloperName = p.Key,
            TemplateVariable = p.Value
        });

        templateVariableDictionary.Add(InsertVariableTemplateFactory.CurrentOrganization.VariableCategoryName, currentOrgVariables);
        templateVariableDictionary.Add(InsertVariableTemplateFactory.CurrentUser.VariableCategoryName, currentUserVariables);

        if (!isBuiltInTemplate)
        {
            var contentTypeVariables = InsertVariableTemplateFactory.ContentType.TemplateInfo.GetTemplateVariables().Select(p => new InsertVariableListItem_ViewModel
            {
                DeveloperName = p.Key,
                TemplateVariable = p.Value
            });

            var builtInContentItemVariables = InsertVariableTemplateFactory.ContentItem.TemplateInfo.GetTemplateVariables().Select(p => new InsertVariableListItem_ViewModel
            {
                DeveloperName = p.Key,
                TemplateVariable = p.Value
            });

            var listResultVariables = InsertVariableTemplateFactory.ContentItemListResult.TemplateInfo.GetTemplateVariables().Select(p => new InsertVariableListItem_ViewModel
            {
                DeveloperName = p.Key,
                TemplateVariable = p.Value
            });


            templateVariableDictionary.Add(InsertVariableTemplateFactory.ContentType.VariableCategoryName, contentTypeVariables);
            templateVariableDictionary.Add($"{InsertVariableTemplateFactory.ContentItemListResult.VariableCategoryName} (list result)", listResultVariables);
            templateVariableDictionary.Add($"{InsertVariableTemplateFactory.ContentItem.VariableCategoryName} (single item)", builtInContentItemVariables);

            foreach (var item in contentTypes)
            {
                var allCustomVariables = item.ContentTypeFields.Select(p => new InsertVariableListItem_ViewModel
                {
                    DeveloperName = $"{p.DeveloperName}{RenderValueProperty(p.FieldType)}",
                    TemplateVariable = $"{InsertVariableTemplateFactory.ContentItem.VariableCategoryName}.PublishedContent.{p.DeveloperName}{RenderValueProperty(p.FieldType)}"
                }).ToList();
                allCustomVariables.AddRange(item.ContentTypeFields.Where(p => p.FieldType.DeveloperName != BaseFieldType.OneToOneRelationship.DeveloperName).Select(p => new InsertVariableListItem_ViewModel
                {
                    DeveloperName = $"{p.DeveloperName}.Value",
                    TemplateVariable = $"{InsertVariableTemplateFactory.ContentItem.VariableCategoryName}.PublishedContent.{p.DeveloperName}.Value"
                }));
                templateVariableDictionary.Add($"{item.LabelSingular}", allCustomVariables.OrderBy(p => p.DeveloperName));
            }
        }
        else
        {
            var webTemplateVariables = InsertVariableTemplateFactory.From(templateName).TemplateInfo.GetTemplateVariables().Select(p => new InsertVariableListItem_ViewModel
            {
                DeveloperName = p.Key,
                TemplateVariable = p.Value
            });
            templateVariableDictionary.Add(InsertVariableTemplateFactory.From(templateName).VariableCategoryName, webTemplateVariables);
        }

        return templateVariableDictionary;
    }

    protected string RenderValueProperty(BaseFieldType fieldType)
    {
        return fieldType.DeveloperName != BaseFieldType.OneToOneRelationship.DeveloperName ? $".Text" : string.Empty;
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        base.OnActionExecuted(context);
        ViewData["ActiveMenu"] = "Templates";
    }
}
