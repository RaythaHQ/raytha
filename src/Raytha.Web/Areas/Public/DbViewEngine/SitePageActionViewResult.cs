using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Primitives;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models.RenderModels;
using Raytha.Application.Common.Utils;
using Raytha.Application.SitePages;
using Raytha.Application.Themes.WebTemplates;

namespace Raytha.Web.Areas.Public.DbViewEngine;

/// <summary>
/// Action result for rendering Site Pages with widget support.
/// </summary>
public class SitePageActionViewResult : IActionResult
{
    private readonly WebTemplateDto _webTemplate;
    private readonly SitePage_RenderModel _sitePage;
    private readonly SitePageDto _sitePageDto;
    private readonly ViewDataDictionary _viewDictionary;
    private readonly bool _previewDraft;

    public SitePageActionViewResult(
        WebTemplateDto webTemplate,
        SitePage_RenderModel sitePage,
        SitePageDto sitePageDto,
        ViewDataDictionary viewDictionary,
        bool previewDraft = false
    )
    {
        _webTemplate = webTemplate;
        _sitePage = sitePage;
        _sitePageDto = sitePageDto;
        _viewDictionary = viewDictionary;
        _previewDraft = previewDraft;
    }

    public string ContentType { get; set; } = "text/html";

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var httpContext = context.HttpContext;
        var renderer = httpContext.RequestServices.GetRequiredService<IRenderEngine>();
        var currentOrg = httpContext.RequestServices.GetRequiredService<ICurrentOrganization>();
        var currentUser = httpContext.RequestServices.GetRequiredService<ICurrentUser>();
        var antiforgery = httpContext.RequestServices.GetRequiredService<IAntiforgery>();

        httpContext.Response.StatusCode = 200;
        httpContext.Response.ContentType = ContentType;

        var sourceWithParents = WebTemplateExtensions.ContentAssembledFromParents(
            _webTemplate.Content,
            _webTemplate.ParentTemplate
        );

        var renderModel = new Wrapper_RenderModel
        {
            CurrentOrganization = CurrentOrganization_RenderModel.GetProjection(currentOrg),
            CurrentUser = CurrentUser_RenderModel.GetProjection(currentUser),
            Target = _sitePage,
            QueryParams = QueryCollectionToDictionary(httpContext.Request.Query),
            RequestVerificationToken = antiforgery.GetAndStoreTokens(httpContext).RequestToken,
            ViewData = _viewDictionary,
            PathBase = currentOrg.PathBase,
        };

        // Use draft widgets if previewing draft, otherwise use published widgets
        var widgetsSource = _previewDraft ? _sitePageDto.Widgets : _sitePageDto.PublishedWidgets;
        var widgetsForRender = widgetsSource.ToDictionary(
            kvp => kvp.Key,
            kvp =>
                kvp.Value
                    .Select(w => new SitePageWidgetRenderData
                    {
                        Id = w.Id,
                        WidgetType = w.WidgetType,
                        SettingsJson = w.SettingsJson,
                        Row = w.Row,
                        Column = w.Column,
                        ColumnSpan = w.ColumnSpan,
                        CssClass = w.CssClass,
                        HtmlId = w.HtmlId,
                        CustomAttributes = w.CustomAttributes,
                    })
                    .ToList()
        );

        await using (var sw = new StreamWriter(httpContext.Response.Body))
        {
            // Use the overload that supports Site Page widgets
            var body = renderer.RenderAsHtml(
                sourceWithParents,
                renderModel,
                currentOrg.ActiveThemeId,
                widgetsForRender
            );
            await sw.WriteAsync(body);
        }
    }

    private static Dictionary<string, string> QueryCollectionToDictionary(IQueryCollection query)
    {
        var dict = new Dictionary<string, string>();
        foreach (var key in query.Keys)
        {
            query.TryGetValue(key, out StringValues value);
            dict.Add(key, value!);
        }
        return dict;
    }
}

