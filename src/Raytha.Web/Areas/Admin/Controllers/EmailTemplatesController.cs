using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Raytha.Application.EmailTemplates.Commands;
using Raytha.Application.EmailTemplates.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Views.EmailTemplates;
using Raytha.Web.Areas.Admin.Views.Shared.ViewModels;
using Raytha.Web.Filters;
using Raytha.Web.Utils;

namespace Raytha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION)]
public class EmailTemplatesController : BaseController
{
    [ServiceFilter(typeof(SetPaginationInformationFilterAttribute))]
    [Route(RAYTHA_ROUTE_PREFIX + "/templates/email", Name = "emailtemplatesindex")]
    public async Task<IActionResult> Index(string search = "", string orderBy = $"Subject {SortOrder.ASCENDING}", int pageNumber = 1, int pageSize = 50)
    {
        var input = new GetEmailTemplates.Query
        {
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy
        };

        var response = await Mediator.Send(input);

        var items = response.Result.Items.Select(p => new EmailTemplatesListItem_ViewModel
        {
            Id = p.Id,
            Subject = p.Subject,
            DeveloperName = p.DeveloperName,
            LastModificationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(p.LastModificationTime),
            LastModifierUser = p.LastModifierUser != null ? p.LastModifierUser.FullName : "N/A",   
        });

        var viewModel = new List_ViewModel<EmailTemplatesListItem_ViewModel>(items, response.Result.TotalCount);

        return View("~/Areas/Admin/Views/EmailTemplates/Index.cshtml", viewModel);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/templates/email/edit/{id}", Name = "emailtemplatesedit")]
    public async Task<IActionResult> Edit(string id)
    {
        var response = await Mediator.Send(new GetEmailTemplateById.Query { Id = id });

        var templateVariableDictionary = GetInsertVariablesViewModel(response.Result.DeveloperName);
        var model = new EmailTemplatesEdit_ViewModel
        {
            Id = response.Result.Id,
            Content = response.Result.Content,
            Subject = response.Result.Subject,
            DeveloperName = response.Result.DeveloperName,
            Cc = response.Result.Cc,
            Bcc = response.Result.Bcc,
            TemplateVariables = templateVariableDictionary,
            AllowedMimeTypes = FileStorageProviderSettings.AllowedMimeTypes,
            MaxFileSize = FileStorageProviderSettings.MaxFileSize,
            UseDirectUploadToCloud = FileStorageProviderSettings.UseDirectUploadToCloud,
            PathBase = CurrentOrganization.PathBase
        };

        return View("~/Areas/Admin/Views/EmailTemplates/Edit.cshtml", model);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/templates/email/edit/{id}", Name = "emailtemplatesedit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EmailTemplatesEdit_ViewModel model, string id)
    {
        var input = new EditEmailTemplate.Command
        {
            Id = id,
            Subject = model.Subject,
            Content = model.Content,
            Bcc = model.Bcc,
            Cc= model.Cc
        };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{model.Subject} was updated successfully.");
            return RedirectToAction("Edit", new { id });
        }
        else
        {
            var templateVariableDictionary = GetInsertVariablesViewModel(model.DeveloperName);
            model.TemplateVariables = templateVariableDictionary;
            model.AllowedMimeTypes = FileStorageProviderSettings.AllowedMimeTypes;
            model.MaxFileSize = FileStorageProviderSettings.MaxFileSize;
            model.UseDirectUploadToCloud = FileStorageProviderSettings.UseDirectUploadToCloud;
            model.PathBase = CurrentOrganization.PathBase;
            SetErrorMessage("There was an error attempting to update this template. See the error below.", response.GetErrors());
            return View("~/Areas/Admin/Views/EmailTemplates/Edit.cshtml", model);
        }
    }

    [ServiceFilter(typeof(SetPaginationInformationFilterAttribute))]
    [Route(RAYTHA_ROUTE_PREFIX + "/templates/email/edit/{id}/revisions", Name = "emailtemplatesrevisionsindex")]
    public async Task<IActionResult> Revisions(string id, string orderBy = $"CreationTime {SortOrder.DESCENDING}", int pageNumber = 1, int pageSize = 50)
    {
        var template = await Mediator.Send(new GetEmailTemplateById.Query { Id = id });

        var input = new GetEmailTemplateRevisionsByTemplateId.Query
        {
            Id = id,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy,
        };

        var response = await Mediator.Send(input);
        var items = response.Result.Items.Select(p => new EmailTemplatesRevisionsListItem_ViewModel
        {
            Id = p.Id,
            Subject = p.Subject,
            CreationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(p.CreationTime),
            CreatorUser = p.CreatorUser != null ? p.CreatorUser.FullName : "N/A",
            Content = p.Content
        });

        var viewModel = new EmailTemplatesRevisionsPagination_ViewModel(items, response.Result.TotalCount)
        {
            EmailTemplateId = template.Result.Id,
        };

        return View("~/Areas/Admin/Views/EmailTemplates/Revisions.cshtml", viewModel);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/templates/email/edit/{id}/revisions/{revisionId}", Name = "emailtemplatesrevisionsrevert")]
    [HttpPost]
    public async Task<IActionResult> RevisionsRevert(string id, string revisionId)
    {
        var input = new RevertEmailTemplate.Command { Id = revisionId };
        var response = await Mediator.Send(input);
        if (response.Success)
            SetSuccessMessage($"Template has been reverted.");
        else
            SetErrorMessage("There was an error reverting this template", response.GetErrors());
        return RedirectToAction("Edit", new { id });
    }

    protected Dictionary<string, IEnumerable<EmailInsertVariableListItem_ViewModel>> GetInsertVariablesViewModel(string emailTemplate)
    {
        var templateVariableDictionary = new Dictionary<string, IEnumerable<EmailInsertVariableListItem_ViewModel>>();

        var currentOrgVariables = InsertVariableTemplateFactory.CurrentOrganization.TemplateInfo.GetTemplateVariables().Select(p => new EmailInsertVariableListItem_ViewModel
        {
            DeveloperName = p.Key,
            TemplateVariable = p.Value
        });

        var emailTemplateVariables = InsertVariableTemplateFactory.From(emailTemplate).TemplateInfo.GetTemplateVariables().Select(p => new EmailInsertVariableListItem_ViewModel
        {
            DeveloperName = p.Key,
            TemplateVariable = p.Value
        });

        templateVariableDictionary.Add(InsertVariableTemplateFactory.CurrentOrganization.VariableCategoryName, currentOrgVariables);
        templateVariableDictionary.Add(InsertVariableTemplateFactory.From(emailTemplate).VariableCategoryName, emailTemplateVariables);

        return templateVariableDictionary;
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        base.OnActionExecuted(context);
        ViewData["ActiveMenu"] = "Templates";
    }
}
