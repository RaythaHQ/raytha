using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Themes.WebTemplates.Commands;
using Raytha.Application.Themes.WebTemplates.Queries;
using Raytha.Application.Views;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.ContentItems;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION)]
public class Revisions : BaseHasFavoriteViewsPageModel, IHasListView<object>, ISubActionViewModel
{
    public ListViewModel<object> ListView { get; set; }
    ViewDto ISubActionViewModel.CurrentView => base.CurrentView;
    public string Id { get; set; }

    public async Task<IActionResult> OnGet(
        string themeId,
        string id,
        string orderBy = $"CreationTime {SortOrder.DESCENDING}",
        int pageNumber = 1,
        int pageSize = 50
    )
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
        var items = response.Result.Items.Select(p => new WebTemplatesRevisionsListItemViewModel
        {
            Id = p.Id,
            Label = p.Label,
            CreationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                p.CreationTime
            ),
            CreatorUser = p.CreatorUser != null ? p.CreatorUser.FullName : "N/A",
            Content = p.Content,
        });

        Id = id;

        return Page();
    }

    public async Task<IActionResult> OnPostRevert(string themeId, string id, string revisionId)
    {
        var input = new RevertWebTemplate.Command { Id = revisionId };
        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage($"Template has been reverted.");
        }
        else
        {
            SetErrorMessage("There was an error reverting this template", response.GetErrors());
        }
        return RedirectToPage(
            "/ContentItems/Edit",
            new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName, id }
        );
    }

    public record WebTemplatesRevisionsPaginationViewModel : PaginationViewModel
    {
        public IEnumerable<WebTemplatesRevisionsListItemViewModel> Items { get; }
        public string TemplateId { get; set; }
        public bool IsBuiltInTemplate { get; set; }
        public string ThemeId { get; set; }

        public WebTemplatesRevisionsPaginationViewModel(
            IEnumerable<WebTemplatesRevisionsListItemViewModel> items,
            int totalCount
        )
            : base(totalCount) => Items = items;
    }

    public record WebTemplatesRevisionsListItemViewModel
    {
        public string Id { get; init; }

        [Display(Name = "Created at")]
        public string CreationTime { get; init; }

        [Display(Name = "Label")]
        public string Label { get; init; }

        [Display(Name = "Created by")]
        public string CreatorUser { get; init; }

        [Display(Name = "Content")]
        public string Content { get; init; }
    }
}
