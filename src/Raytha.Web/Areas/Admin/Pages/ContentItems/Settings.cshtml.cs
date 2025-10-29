using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.ContentItems;
using Raytha.Application.ContentItems.Commands;
using Raytha.Application.ContentItems.Queries;
using Raytha.Application.Themes.WebTemplates.Queries;
using Raytha.Application.Views;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.ContentItems;

[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
public class Settings : BaseHasFavoriteViewsPageModel, ISubActionViewModel
{
    public string Id { get; set; }
    ViewDto ISubActionViewModel.CurrentView => base.CurrentView;
    private FieldValueConverter _fieldValueConverter;

    protected FieldValueConverter FieldValueConverter =>
        _fieldValueConverter ??=
            HttpContext.RequestServices.GetRequiredService<FieldValueConverter>();
    public string WebsiteUrl { get; set; }
    public Dictionary<string, string> ContentTypeFields { get; set; }

    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet(string id)
    {
        var response = await Mediator.Send(new GetContentItemById.Query { Id = id });

        var webTemplates = await Mediator.Send(
            new GetWebTemplates.Query
            {
                ThemeId = CurrentOrganization.ActiveThemeId,
                ContentTypeId = CurrentView.ContentTypeId,
                PageSize = int.MaxValue,
            }
        );

        var webTemplateResponse = await Mediator.Send(
            new GetWebTemplateByContentItemId.Query
            {
                ContentItemId = id,
                ThemeId = CurrentOrganization.ActiveThemeId,
            }
        );

        Form = new FormModel
        {
            Id = response.Result.Id,
            TemplateId = webTemplateResponse.Result.Id,
            IsHomePage = CurrentOrganization.HomePageId == response.Result.Id,
            RoutePath = response.Result.RoutePath,
            WebsiteUrl =
                CurrentOrganization.WebsiteUrl.TrimEnd('/') + CurrentOrganization.PathBase + "/",
            AvailableTemplates = webTemplates.Result?.Items.ToDictionary(
                p => p.Id.ToString(),
                p => p.Label
            ),
        };

        Id = id;

        return Page();
    }

    public async Task<IActionResult> OnPost(string id)
    {
        var input = new EditContentItemSettings.Command
        {
            Id = id,
            TemplateId = Form.TemplateId,
            RoutePath = Form.RoutePath,
        };

        var editResponse = await Mediator.Send(input);
        if (editResponse.Success)
        {
            SetSuccessMessage($"{CurrentView.ContentType.LabelSingular} was edited successfully.");
            return RedirectToPage(
                "/ContentItems/Settings",
                new { id, contentTypeDeveloperName = CurrentView.ContentType.DeveloperName }
            );
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to save this page. See the error below.",
                editResponse.GetErrors()
            );

            var webTemplatesResponse = await Mediator.Send(
                new GetWebTemplates.Query
                {
                    ThemeId = CurrentOrganization.ActiveThemeId,
                    ContentTypeId = CurrentView.ContentTypeId,
                    PageSize = int.MaxValue,
                }
            );

            Form.AvailableTemplates = webTemplatesResponse.Result?.Items.ToDictionary(
                p => p.Id.ToString(),
                p => p.Label
            );
            Form.WebsiteUrl =
                CurrentOrganization.WebsiteUrl.TrimEnd('/') + CurrentOrganization.PathBase + "/";

            Id = id;
            return Page();
        }
    }

    public async Task<IActionResult> OnPostSetAsHomePage(string id)
    {
        return RedirectToPage("/ContentItems/Settings", new { id });
    }

    public record FormModel
    {
        public string Id { get; set; }

        [Display(Name = "Template")]
        public string TemplateId { get; set; }

        [Display(Name = "Route path")]
        public string RoutePath { get; set; }

        public bool IsHomePage { get; set; }

        //helpers
        public string WebsiteUrl { get; set; }
        public Dictionary<string, string> AvailableTemplates { get; set; }
    }
}
