using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.ContentItems;
using Raytha.Application.ContentItems.Queries;
using Raytha.Application.Themes.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.ContentItems;

[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
public class Index
    : BaseHasFavoriteViewsPageModel,
        IHasListView<Index.ContentItemsListItemViewModel>
{
    private FieldValueConverter _fieldValueConverter;

    protected FieldValueConverter FieldValueConverter =>
        _fieldValueConverter ??=
            HttpContext.RequestServices.GetRequiredService<FieldValueConverter>();
    public ListViewModel<ContentItemsListItemViewModel> ListView { get; set; }

    public async Task<IActionResult> OnGet(
        string search = "",
        string filter = "",
        string orderBy = "",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        var input = new GetContentItems.Query
        {
            Search = search,
            ViewId = CurrentView.Id,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy,
            Filter = filter,
        };
        var response = await Mediator.Send(input);

        var webTemplateContentItemRelationsResponse = await Mediator.Send(
            new GetWebTemplateContentItemRelationsByContentTypeId.Query
            {
                ThemeId = CurrentOrganization.ActiveThemeId,
                ContentTypeId = CurrentView.ContentTypeId,
            }
        );

        var items = response.Result.Items.Select(p =>
        {
            var templateLabel = webTemplateContentItemRelationsResponse
                .Result.Where(wtr => wtr.ContentItemId == p.Id)
                .Select(wtr => wtr.WebTemplate.Label)
                .First();
            return new ContentItemsListItemViewModel
            {
                Id = p.Id,
                IsHomePage = CurrentOrganization.HomePageId == p.Id,
                FieldValues = FieldValueConverter.MapToListItemValues(p, templateLabel),
            };
        });

        ListView = new ListViewModel<ContentItemsListItemViewModel>(
            items,
            response.Result.TotalCount
        );
        return Page();
    }

    public record ContentItemsListItemViewModel
    {
        public string Id { get; set; }
        public bool IsHomePage { get; set; }
        public Dictionary<string, string> FieldValues { get; set; }
    }
}
