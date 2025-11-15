using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.EmailTemplates.Commands;
using Raytha.Application.EmailTemplates.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;
using Raytha.Web.Utils;

namespace Raytha.Web.Areas.Admin.Pages.EmailTemplates;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class Edit : BaseAdminPageModel, ISubActionViewModel
{
    [BindProperty]
    public FormModel Form { get; set; }
    public string Id { get; set; }

    public Dictionary<
        string,
        IEnumerable<IEmailTemplatesInsertVariableListItemViewModel>
    > TemplateVariables
    { get; set; }

    public async Task<IActionResult> OnGet(string id)
    {
        // Set breadcrumbs for navigation
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Email Templates",
                RouteName = RouteNames.EmailTemplates.Index,
                IsActive = false,
                Icon = SidebarIcons.EmailTemplates,
            },
            new BreadcrumbNode
            {
                Label = "Edit",
                RouteName = RouteNames.EmailTemplates.Edit,
                IsActive = true,
            }
        );

        var response = await Mediator.Send(new GetEmailTemplateById.Query { Id = id });

        TemplateVariables = GetInsertVariablesViewModel(response.Result.DeveloperName);

        Form = new FormModel
        {
            Id = response.Result.Id,
            Content = response.Result.Content,
            Subject = response.Result.Subject,
            DeveloperName = response.Result.DeveloperName,
            Cc = response.Result.Cc,
            Bcc = response.Result.Bcc,
        };
        Id = response.Result.Id;
        return Page();
    }

    public async Task<IActionResult> OnPost(string id)
    {
        var template = await Mediator.Send(new GetEmailTemplateById.Query { Id = id });
        TemplateVariables = GetInsertVariablesViewModel(template.Result.DeveloperName);
        var input = new EditEmailTemplate.Command
        {
            Id = id,
            Subject = Form.Subject,
            Content = Form.Content,
            Bcc = Form.Bcc,
            Cc = Form.Cc,
        };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{Form.Subject} was updated successfully.");
            return RedirectToPage(RouteNames.EmailTemplates.Edit, new { id });
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to update this template. See the error below.",
                response.GetErrors()
            );
            Id = Form.Id;
            return Page();
        }
    }

    protected Dictionary<
        string,
        IEnumerable<IEmailTemplatesInsertVariableListItemViewModel>
    > GetInsertVariablesViewModel(string emailTemplate)
    {
        var templateVariableDictionary =
            new Dictionary<string, IEnumerable<IEmailTemplatesInsertVariableListItemViewModel>>();

        var currentOrgVariables = InsertVariableTemplateFactory
            .CurrentOrganization.TemplateInfo.GetTemplateVariables()
            .Select(p => new EmailInsertVariableListItemViewModel
            {
                DeveloperName = p.Key,
                TemplateVariable = p.Value,
            });

        var emailTemplateVariables = InsertVariableTemplateFactory
            .From(emailTemplate)
            .TemplateInfo.GetTemplateVariables()
            .Select(p => new EmailInsertVariableListItemViewModel
            {
                DeveloperName = p.Key,
                TemplateVariable = p.Value,
            });

        templateVariableDictionary.Add(
            InsertVariableTemplateFactory.CurrentOrganization.VariableCategoryName,
            currentOrgVariables
        );
        templateVariableDictionary.Add(
            InsertVariableTemplateFactory.From(emailTemplate).VariableCategoryName,
            emailTemplateVariables
        );

        return templateVariableDictionary;
    }

    public class EmailInsertVariableListItemViewModel
        : IEmailTemplatesInsertVariableListItemViewModel
    {
        public string DeveloperName { get; set; }
        public string TemplateVariable { get; set; }
    }

    public record FormModel
    {
        public string Id { get; set; }

        [Display(Name = "Subject")]
        public string Subject { get; set; }

        [Display(Name = "Developer name")]
        public string DeveloperName { get; set; }

        [Display(Name = "Content")]
        public string Content { get; set; }

        [Display(Name = "Cc")]
        public string Cc { get; set; }

        [Display(Name = "Bcc")]
        public string Bcc { get; set; }
    }
}
