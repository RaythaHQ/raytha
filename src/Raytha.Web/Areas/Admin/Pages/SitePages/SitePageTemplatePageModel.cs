using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.SitePages;

public abstract class SitePageTemplatePageModel : BaseAdminPageModel
{
    private readonly ISitePageTemplateOptionsProvider _templateOptionsProvider;

    protected SitePageTemplatePageModel(
        ISitePageTemplateOptionsProvider templateOptionsProvider
    )
    {
        _templateOptionsProvider = templateOptionsProvider;
    }

    public IEnumerable<SelectListItem> AvailableTemplates { get; private set; } =
        Enumerable.Empty<SelectListItem>();

    protected async Task LoadTemplateOptionsAsync(CancellationToken cancellationToken)
    {
        AvailableTemplates = await _templateOptionsProvider.GetTemplatesAsync(
            CurrentOrganization.ActiveThemeId,
            cancellationToken
        );
    }
}

