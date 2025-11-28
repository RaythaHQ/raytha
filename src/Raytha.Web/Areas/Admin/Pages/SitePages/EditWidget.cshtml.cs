using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Application.SitePages.Commands;
using Raytha.Application.SitePages.Queries;
using Raytha.Application.SitePages.Widgets;
using Raytha.Application.SitePages.Widgets.Settings;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.SitePages;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SITE_PAGES_PERMISSION)]
public class EditWidget : BaseAdminPageModel
{
    public string SitePageId { get; set; }
    public string SitePageTitle { get; set; }
    public string WidgetId { get; set; }
    public string WidgetType { get; set; }
    public string WidgetDisplayName { get; set; }
    public string SectionName { get; set; } = "main";

    [BindProperty]
    public HeroFormModel HeroForm { get; set; }

    [BindProperty]
    public WysiwygFormModel WysiwygForm { get; set; }

    [BindProperty]
    public CtaFormModel CtaForm { get; set; }

    [BindProperty]
    public ImageTextFormModel ImageTextForm { get; set; }

    [BindProperty]
    public EmbedFormModel EmbedForm { get; set; }

    [BindProperty]
    public CardGridFormModel CardGridForm { get; set; }

    [BindProperty]
    public FaqFormModel FaqForm { get; set; }

    [BindProperty]
    public ContentListFormModel ContentListForm { get; set; }

    [BindProperty]
    public AdvancedMetaFormModel AdvancedMeta { get; set; } = new AdvancedMetaFormModel();

    // For content list widget - content types dropdown
    public IEnumerable<SelectListItem> ContentTypes { get; set; } = Enumerable.Empty<SelectListItem>();

    public async Task<IActionResult> OnGet(
        string sitePageId,
        string widgetId,
        string widgetType,
        string sectionName = "main"
    )
    {
        var response = await Mediator.Send(new GetSitePageById.Query { Id = sitePageId });

        SitePageId = sitePageId;
        SitePageTitle = response.Result.Title;
        WidgetId = widgetId;
        WidgetType = widgetType;
        SectionName = sectionName;

        var definition = WidgetDefinitionService.GetByDeveloperName(widgetType);
        WidgetDisplayName = definition?.DisplayName ?? widgetType;

        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Site Pages",
                RouteName = RouteNames.SitePages.Index,
                IsActive = false,
                Icon = SidebarIcons.SitePages,
            },
            new BreadcrumbNode
            {
                Label = response.Result.Title,
                RouteName = RouteNames.SitePages.Layout,
                RouteValues = new Dictionary<string, string> { ["id"] = sitePageId },
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = $"Edit {WidgetDisplayName}",
                RouteName = RouteNames.SitePages.EditWidget,
                IsActive = true,
            }
        );

        // Find existing widget settings if editing
        var existingSettings = "{}";
        if (!string.IsNullOrEmpty(widgetId) && widgetId != "new")
        {
            var widgets = response.Result.Widgets.GetValueOrDefault(sectionName);
            var widget = widgets?.FirstOrDefault(w => w.Id == widgetId);
            if (widget != null)
            {
                existingSettings = widget.SettingsJson ?? "{}";
                // Load advanced meta fields
                AdvancedMeta = new AdvancedMetaFormModel
                {
                    CssClass = widget.CssClass,
                    HtmlId = widget.HtmlId,
                    CustomAttributes = widget.CustomAttributes,
                };
            }
        }

        // Initialize the appropriate form based on widget type
        await InitializeForm(widgetType, existingSettings);

        return Page();
    }

    public async Task<IActionResult> OnPost(
        string sitePageId,
        string widgetId,
        string widgetType,
        string sectionName = "main"
    )
    {
        var response = await Mediator.Send(new GetSitePageById.Query { Id = sitePageId });

        SitePageId = sitePageId;
        SitePageTitle = response.Result.Title;
        WidgetId = widgetId;
        WidgetType = widgetType;
        SectionName = sectionName;

        var definition = WidgetDefinitionService.GetByDeveloperName(widgetType);
        WidgetDisplayName = definition?.DisplayName ?? widgetType;

        // Serialize the appropriate form to JSON
        var settingsJson = SerializeFormToJson(widgetType);

        // Get current widgets for the section
        var currentWidgets = response.Result.Widgets.GetValueOrDefault(sectionName)?.ToList()
            ?? new List<Application.SitePages.SitePageWidgetDto>();

        // Find existing widget or create new
        var isNewWidget = string.IsNullOrEmpty(widgetId) || widgetId == "new";
        var actualWidgetId = isNewWidget ? Guid.NewGuid().ToString() : widgetId;

        if (isNewWidget)
        {
            // Add new widget at the end
            currentWidgets.Add(new Application.SitePages.SitePageWidgetDto
            {
                Id = actualWidgetId,
                WidgetType = widgetType,
                SettingsJson = settingsJson,
                Row = currentWidgets.Count,
                Column = 0,
                ColumnSpan = 12,
                CssClass = AdvancedMeta?.CssClass ?? string.Empty,
                HtmlId = AdvancedMeta?.HtmlId ?? string.Empty,
                CustomAttributes = AdvancedMeta?.CustomAttributes ?? string.Empty,
            });
        }
        else
        {
            // Update existing widget settings and meta fields
            var existingIndex = currentWidgets.FindIndex(w => w.Id == widgetId);
            if (existingIndex >= 0)
            {
                var existing = currentWidgets[existingIndex];
                currentWidgets[existingIndex] = existing with
                {
                    SettingsJson = settingsJson,
                    CssClass = AdvancedMeta?.CssClass ?? string.Empty,
                    HtmlId = AdvancedMeta?.HtmlId ?? string.Empty,
                    CustomAttributes = AdvancedMeta?.CustomAttributes ?? string.Empty,
                };
            }
        }

        // Save widgets
        var saveInput = new SaveWidgets.Command
        {
            Id = sitePageId,
            SectionName = sectionName,
            Widgets = currentWidgets.Select(w => new SaveWidgets.WidgetInput
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
            }),
        };

        var saveResponse = await Mediator.Send(saveInput);

        if (saveResponse.Success)
        {
            SetSuccessMessage($"{WidgetDisplayName} widget saved successfully.");
            return RedirectToPage(RouteNames.SitePages.Layout, new { id = sitePageId });
        }
        else
        {
            SetErrorMessage("Error saving widget.", saveResponse.GetErrors());
            await InitializeForm(widgetType, settingsJson);
            return Page();
        }
    }

    private async Task InitializeForm(string widgetType, string settingsJson)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        switch (widgetType)
        {
            case "hero":
                var heroSettings =
                    JsonSerializer.Deserialize<HeroWidgetSettings>(settingsJson, options)
                    ?? new HeroWidgetSettings();
                HeroForm = new HeroFormModel
                {
                    Headline = heroSettings.Headline,
                    Subheadline = heroSettings.Subheadline,
                    BackgroundImage = heroSettings.BackgroundImage,
                    BackgroundColor = heroSettings.BackgroundColor,
                    TextColor = heroSettings.TextColor,
                    ButtonText = heroSettings.ButtonText,
                    ButtonUrl = heroSettings.ButtonUrl,
                    ButtonStyle = heroSettings.ButtonStyle,
                    Alignment = heroSettings.Alignment,
                    MinHeight = heroSettings.MinHeight,
                };
                break;

            case "wysiwyg":
                var wysiwygSettings =
                    JsonSerializer.Deserialize<WysiwygWidgetSettings>(settingsJson, options)
                    ?? new WysiwygWidgetSettings();
                WysiwygForm = new WysiwygFormModel
                {
                    Content = wysiwygSettings.Content,
                    BackgroundColor = wysiwygSettings.BackgroundColor,
                    Padding = wysiwygSettings.Padding,
                };
                break;

            case "cta":
                var ctaSettings =
                    JsonSerializer.Deserialize<CtaWidgetSettings>(settingsJson, options)
                    ?? new CtaWidgetSettings();
                CtaForm = new CtaFormModel
                {
                    Headline = ctaSettings.Headline,
                    Content = ctaSettings.Content,
                    ButtonText = ctaSettings.ButtonText,
                    ButtonUrl = ctaSettings.ButtonUrl,
                    ButtonStyle = ctaSettings.ButtonStyle,
                    BackgroundColor = ctaSettings.BackgroundColor,
                    TextColor = ctaSettings.TextColor,
                    Alignment = ctaSettings.Alignment,
                };
                break;

            case "imagetext":
                var imageTextSettings =
                    JsonSerializer.Deserialize<ImageTextWidgetSettings>(settingsJson, options)
                    ?? new ImageTextWidgetSettings();
                ImageTextForm = new ImageTextFormModel
                {
                    ImageUrl = imageTextSettings.ImageUrl,
                    ImageAlt = imageTextSettings.ImageAlt,
                    Headline = imageTextSettings.Headline,
                    Content = imageTextSettings.Content,
                    ImagePosition = imageTextSettings.ImagePosition,
                    ButtonText = imageTextSettings.ButtonText,
                    ButtonUrl = imageTextSettings.ButtonUrl,
                    ButtonStyle = imageTextSettings.ButtonStyle,
                    BackgroundColor = imageTextSettings.BackgroundColor,
                };
                break;

            case "embed":
                var embedSettings =
                    JsonSerializer.Deserialize<EmbedWidgetSettings>(settingsJson, options)
                    ?? new EmbedWidgetSettings();
                EmbedForm = new EmbedFormModel
                {
                    EmbedType = embedSettings.EmbedType,
                    IframeUrl = embedSettings.IframeUrl,
                    HtmlContent = embedSettings.HtmlContent,
                    AspectRatio = embedSettings.AspectRatio,
                    MaxWidth = embedSettings.MaxWidth,
                    Caption = embedSettings.Caption,
                    BackgroundColor = embedSettings.BackgroundColor,
                };
                break;

            case "cardgrid":
                var cardGridSettings =
                    JsonSerializer.Deserialize<CardGridWidgetSettings>(settingsJson, options)
                    ?? new CardGridWidgetSettings();
                CardGridForm = new CardGridFormModel
                {
                    Headline = cardGridSettings.Headline,
                    Subheadline = cardGridSettings.Subheadline,
                    Columns = cardGridSettings.Columns,
                    BackgroundColor = cardGridSettings.BackgroundColor,
                    CardsJson = JsonSerializer.Serialize(cardGridSettings.Cards),
                };
                break;

            case "faq":
                var faqSettings =
                    JsonSerializer.Deserialize<FaqWidgetSettings>(settingsJson, options)
                    ?? new FaqWidgetSettings();
                FaqForm = new FaqFormModel
                {
                    Headline = faqSettings.Headline,
                    Subheadline = faqSettings.Subheadline,
                    BackgroundColor = faqSettings.BackgroundColor,
                    ExpandFirst = faqSettings.ExpandFirst,
                    ItemsJson = JsonSerializer.Serialize(faqSettings.Items),
                };
                break;

            case "contentlist":
                var contentListSettings =
                    JsonSerializer.Deserialize<ContentListWidgetSettings>(settingsJson, options)
                    ?? new ContentListWidgetSettings();
                ContentListForm = new ContentListFormModel
                {
                    Headline = contentListSettings.Headline,
                    Subheadline = contentListSettings.Subheadline,
                    ContentType = contentListSettings.ContentType,
                    Filter = contentListSettings.Filter,
                    OrderBy = contentListSettings.OrderBy,
                    PageSize = contentListSettings.PageSize,
                    DisplayStyle = contentListSettings.DisplayStyle,
                    Columns = contentListSettings.Columns,
                    ShowImage = contentListSettings.ShowImage,
                    ShowDate = contentListSettings.ShowDate,
                    ShowExcerpt = contentListSettings.ShowExcerpt,
                    LinkText = contentListSettings.LinkText,
                    LinkUrl = contentListSettings.LinkUrl,
                    BackgroundColor = contentListSettings.BackgroundColor,
                };

                // Load content types for dropdown
                var contentTypesResponse = await Mediator.Send(new GetContentTypes.Query());
                ContentTypes = contentTypesResponse.Result.Items.Select(ct => new SelectListItem
                {
                    Value = ct.DeveloperName,
                    Text = ct.LabelPlural,
                });
                break;
        }
    }

    private string SerializeFormToJson(string widgetType)
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        return widgetType switch
        {
            "hero" => JsonSerializer.Serialize(
                new HeroWidgetSettings
                {
                    Headline = HeroForm?.Headline ?? string.Empty,
                    Subheadline = HeroForm?.Subheadline ?? string.Empty,
                    BackgroundImage = HeroForm?.BackgroundImage,
                    BackgroundColor = HeroForm?.BackgroundColor ?? "#0d6efd",
                    TextColor = HeroForm?.TextColor ?? "#ffffff",
                    ButtonText = HeroForm?.ButtonText,
                    ButtonUrl = HeroForm?.ButtonUrl,
                    ButtonStyle = HeroForm?.ButtonStyle ?? "light",
                    Alignment = HeroForm?.Alignment ?? "center",
                    MinHeight = HeroForm?.MinHeight ?? 400,
                },
                options
            ),

            "wysiwyg" => JsonSerializer.Serialize(
                new WysiwygWidgetSettings
                {
                    Content = WysiwygForm?.Content ?? string.Empty,
                    BackgroundColor = WysiwygForm?.BackgroundColor,
                    Padding = WysiwygForm?.Padding ?? "medium",
                },
                options
            ),

            "cta" => JsonSerializer.Serialize(
                new CtaWidgetSettings
                {
                    Headline = CtaForm?.Headline ?? string.Empty,
                    Content = CtaForm?.Content,
                    ButtonText = CtaForm?.ButtonText,
                    ButtonUrl = CtaForm?.ButtonUrl,
                    ButtonStyle = CtaForm?.ButtonStyle ?? "light",
                    BackgroundColor = CtaForm?.BackgroundColor ?? "#0d6efd",
                    TextColor = CtaForm?.TextColor ?? "#ffffff",
                    Alignment = CtaForm?.Alignment ?? "center",
                },
                options
            ),

            "imagetext" => JsonSerializer.Serialize(
                new ImageTextWidgetSettings
                {
                    ImageUrl = ImageTextForm?.ImageUrl,
                    ImageAlt = ImageTextForm?.ImageAlt,
                    Headline = ImageTextForm?.Headline ?? string.Empty,
                    Content = ImageTextForm?.Content ?? string.Empty,
                    ImagePosition = ImageTextForm?.ImagePosition ?? "left",
                    ButtonText = ImageTextForm?.ButtonText,
                    ButtonUrl = ImageTextForm?.ButtonUrl,
                    ButtonStyle = ImageTextForm?.ButtonStyle ?? "primary",
                    BackgroundColor = ImageTextForm?.BackgroundColor,
                },
                options
            ),

            "embed" => JsonSerializer.Serialize(
                new EmbedWidgetSettings
                {
                    EmbedType = EmbedForm?.EmbedType ?? "iframe",
                    IframeUrl = EmbedForm?.IframeUrl,
                    HtmlContent = EmbedForm?.HtmlContent,
                    AspectRatio = EmbedForm?.AspectRatio ?? "16x9",
                    MaxWidth = EmbedForm?.MaxWidth,
                    Caption = EmbedForm?.Caption,
                    BackgroundColor = EmbedForm?.BackgroundColor,
                },
                options
            ),

            "cardgrid" => JsonSerializer.Serialize(
                new CardGridWidgetSettings
                {
                    Headline = CardGridForm?.Headline,
                    Subheadline = CardGridForm?.Subheadline,
                    Columns = CardGridForm?.Columns ?? 3,
                    BackgroundColor = CardGridForm?.BackgroundColor,
                    Cards = !string.IsNullOrEmpty(CardGridForm?.CardsJson)
                        ? JsonSerializer.Deserialize<List<CardGridWidgetSettings.CardItem>>(
                              CardGridForm.CardsJson,
                              new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                          ) ?? new List<CardGridWidgetSettings.CardItem>()
                        : new List<CardGridWidgetSettings.CardItem>(),
                },
                options
            ),

            "faq" => JsonSerializer.Serialize(
                new FaqWidgetSettings
                {
                    Headline = FaqForm?.Headline,
                    Subheadline = FaqForm?.Subheadline,
                    BackgroundColor = FaqForm?.BackgroundColor,
                    ExpandFirst = FaqForm?.ExpandFirst ?? true,
                    Items = !string.IsNullOrEmpty(FaqForm?.ItemsJson)
                        ? JsonSerializer.Deserialize<List<FaqWidgetSettings.FaqItem>>(
                              FaqForm.ItemsJson,
                              new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                          ) ?? new List<FaqWidgetSettings.FaqItem>()
                        : new List<FaqWidgetSettings.FaqItem>(),
                },
                options
            ),

            "contentlist" => JsonSerializer.Serialize(
                new ContentListWidgetSettings
                {
                    Headline = ContentListForm?.Headline,
                    Subheadline = ContentListForm?.Subheadline,
                    ContentType = ContentListForm?.ContentType ?? string.Empty,
                    Filter = ContentListForm?.Filter,
                    OrderBy = ContentListForm?.OrderBy ?? "CreationTime desc",
                    PageSize = ContentListForm?.PageSize ?? 6,
                    DisplayStyle = ContentListForm?.DisplayStyle ?? "cards",
                    Columns = ContentListForm?.Columns ?? 3,
                    ShowImage = ContentListForm?.ShowImage ?? true,
                    ShowDate = ContentListForm?.ShowDate ?? true,
                    ShowExcerpt = ContentListForm?.ShowExcerpt ?? true,
                    LinkText = ContentListForm?.LinkText,
                    LinkUrl = ContentListForm?.LinkUrl,
                    BackgroundColor = ContentListForm?.BackgroundColor,
                },
                options
            ),

            _ => "{}",
        };
    }

    // Form Models
    public record HeroFormModel
    {
        [Display(Name = "Headline")]
        public string Headline { get; set; } = string.Empty;

        [Display(Name = "Subheadline")]
        public string Subheadline { get; set; } = string.Empty;

        [Display(Name = "Background Image URL")]
        public string? BackgroundImage { get; set; }

        [Display(Name = "Background Color")]
        public string BackgroundColor { get; set; } = "#0d6efd";

        [Display(Name = "Text Color")]
        public string TextColor { get; set; } = "#ffffff";

        [Display(Name = "Button Text")]
        public string? ButtonText { get; set; }

        [Display(Name = "Button URL")]
        public string? ButtonUrl { get; set; }

        [Display(Name = "Button Style")]
        public string ButtonStyle { get; set; } = "light";

        [Display(Name = "Text Alignment")]
        public string Alignment { get; set; } = "center";

        [Display(Name = "Minimum Height (px)")]
        public int MinHeight { get; set; } = 400;
    }

    public record WysiwygFormModel
    {
        [Display(Name = "Content")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Background Color")]
        public string? BackgroundColor { get; set; }

        [Display(Name = "Padding")]
        public string Padding { get; set; } = "medium";
    }

    public record CtaFormModel
    {
        [Display(Name = "Headline")]
        public string Headline { get; set; } = string.Empty;

        [Display(Name = "Content")]
        public string? Content { get; set; }

        [Display(Name = "Button Text")]
        public string? ButtonText { get; set; }

        [Display(Name = "Button URL")]
        public string? ButtonUrl { get; set; }

        [Display(Name = "Button Style")]
        public string ButtonStyle { get; set; } = "light";

        [Display(Name = "Background Color")]
        public string BackgroundColor { get; set; } = "#0d6efd";

        [Display(Name = "Text Color")]
        public string TextColor { get; set; } = "#ffffff";

        [Display(Name = "Text Alignment")]
        public string Alignment { get; set; } = "center";
    }

    public record ImageTextFormModel
    {
        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Image Alt Text")]
        public string? ImageAlt { get; set; }

        [Display(Name = "Headline")]
        public string Headline { get; set; } = string.Empty;

        [Display(Name = "Content")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Image Position")]
        public string ImagePosition { get; set; } = "left";

        [Display(Name = "Button Text")]
        public string? ButtonText { get; set; }

        [Display(Name = "Button URL")]
        public string? ButtonUrl { get; set; }

        [Display(Name = "Button Style")]
        public string ButtonStyle { get; set; } = "primary";

        [Display(Name = "Background Color")]
        public string? BackgroundColor { get; set; }
    }

    public record EmbedFormModel
    {
        [Display(Name = "Embed Type")]
        public string EmbedType { get; set; } = "iframe";

        [Display(Name = "Iframe URL")]
        public string? IframeUrl { get; set; }

        [Display(Name = "HTML Content")]
        public string? HtmlContent { get; set; }

        [Display(Name = "Aspect Ratio")]
        public string AspectRatio { get; set; } = "16x9";

        [Display(Name = "Max Width (px)")]
        public int? MaxWidth { get; set; }

        [Display(Name = "Caption")]
        public string? Caption { get; set; }

        [Display(Name = "Background Color")]
        public string? BackgroundColor { get; set; }
    }

    public record CardGridFormModel
    {
        [Display(Name = "Headline")]
        public string? Headline { get; set; }

        [Display(Name = "Subheadline")]
        public string? Subheadline { get; set; }

        [Display(Name = "Number of Columns")]
        public int Columns { get; set; } = 3;

        [Display(Name = "Background Color")]
        public string? BackgroundColor { get; set; }

        // Cards stored as JSON for simplicity
        public string CardsJson { get; set; } = "[]";
    }

    public record FaqFormModel
    {
        [Display(Name = "Headline")]
        public string? Headline { get; set; }

        [Display(Name = "Subheadline")]
        public string? Subheadline { get; set; }

        [Display(Name = "Background Color")]
        public string? BackgroundColor { get; set; }

        [Display(Name = "Expand First Item")]
        public bool ExpandFirst { get; set; } = true;

        // Items stored as JSON for simplicity
        public string ItemsJson { get; set; } = "[]";
    }

    public record ContentListFormModel
    {
        [Display(Name = "Headline")]
        public string? Headline { get; set; }

        [Display(Name = "Subheadline")]
        public string? Subheadline { get; set; }

        [Display(Name = "Content Type")]
        public string ContentType { get; set; } = string.Empty;

        [Display(Name = "Filter")]
        public string? Filter { get; set; }

        [Display(Name = "Order By")]
        public string OrderBy { get; set; } = "CreationTime desc";

        [Display(Name = "Number of Items")]
        public int PageSize { get; set; } = 6;

        [Display(Name = "Display Style")]
        public string DisplayStyle { get; set; } = "cards";

        [Display(Name = "Number of Columns")]
        public int Columns { get; set; } = 3;

        [Display(Name = "Show Image")]
        public bool ShowImage { get; set; } = true;

        [Display(Name = "Show Date")]
        public bool ShowDate { get; set; } = true;

        [Display(Name = "Show Excerpt")]
        public bool ShowExcerpt { get; set; } = true;

        [Display(Name = "Link Text")]
        public string? LinkText { get; set; }

        [Display(Name = "Link URL")]
        public string? LinkUrl { get; set; }

        [Display(Name = "Background Color")]
        public string? BackgroundColor { get; set; }
    }

    public record AdvancedMetaFormModel
    {
        [Display(Name = "CSS Class")]
        public string CssClass { get; set; } = string.Empty;

        [Display(Name = "HTML ID")]
        public string HtmlId { get; set; } = string.Empty;

        [Display(Name = "Custom Attributes")]
        public string CustomAttributes { get; set; } = string.Empty;
    }
}

