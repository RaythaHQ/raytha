using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.AspNetCore.Mvc.Rendering;
using Raytha.Application.Themes.WebTemplates.Queries;

namespace Raytha.Web.Areas.Admin.Pages.SitePages;

public interface ISitePageTemplateOptionsProvider
{
    Task<IReadOnlyList<SelectListItem>> GetTemplatesAsync(
        Guid themeId,
        CancellationToken cancellationToken
    );
}

public class SitePageTemplateOptionsProvider : ISitePageTemplateOptionsProvider
{
    private readonly ISender _mediator;

    public SitePageTemplateOptionsProvider(ISender mediator)
    {
        _mediator = mediator;
    }

    public async Task<IReadOnlyList<SelectListItem>> GetTemplatesAsync(
        Guid themeId,
        CancellationToken cancellationToken
    )
    {
        var templatesResponse = await _mediator.Send(
            new GetWebTemplates.Query { ThemeId = themeId },
            cancellationToken
        );

        return templatesResponse.Result.Items
            .Where(t => !t.IsBaseLayout)
            .Select(t => new SelectListItem { Value = t.Id, Text = t.Label })
            .ToList();
    }
}

