using System.Text.Json;
using Raytha.Domain.Entities;

namespace Raytha.Domain.ValueObjects;

/// <summary>
/// Default fields of the built-in widget templates. Each DeveloperName is the exact SettingsJson key
/// the admin form writes and the default Liquid reads as widget.settings.&lt;key&gt;; renaming one
/// orphans every stored widget's value for it.
/// </summary>
internal static class BuiltInWidgetFields
{
    /// <summary>
    /// The 2.x admin offered the first six; 1.5 also offered the rest, so stored widgets can hold any of them.
    /// </summary>
    private static readonly ContentTypeFieldChoice[] ButtonStyles = Choices(
        ("primary", "Primary"),
        ("secondary", "Secondary"),
        ("outline-primary", "Outline primary"),
        ("outline-light", "Outline light"),
        ("light", "Light"),
        ("link", "Link"),
        ("success", "Success"),
        ("danger", "Danger"),
        ("warning", "Warning"),
        ("info", "Info"),
        ("dark", "Dark"),
        ("outline-dark", "Outline dark")
    );

    private static readonly ContentTypeFieldChoice[] Alignments = Choices(
        ("left", "Left"),
        ("center", "Center"),
        ("right", "Right")
    );

    public static readonly IReadOnlyList<FieldDefinition> Hero =
    [
        Text("headline", "Headline"),
        Text("subheadline", "Subheadline"),
        Image("backgroundImage", "Background image"),
        Color("backgroundColor", "Background color", "#1e293b"),
        Color("textColor", "Text color", "#ffffff"),
        Text("buttonText", "Button text"),
        Text("buttonUrl", "Button URL"),
        Dropdown("buttonStyle", "Button style", ButtonStyles, "light"),
        Dropdown("alignment", "Alignment", Alignments, "center"),
        Number("minHeight", "Minimum height", 400, "In pixels."),
    ];

    public static readonly IReadOnlyList<FieldDefinition> Wysiwyg =
    [
        RichText("content", "Content"),
        Color("backgroundColor", "Background color"),
        Dropdown(
            "padding",
            "Padding",
            Choices(("none", "None"), ("small", "Small"), ("medium", "Medium"), ("large", "Large")),
            "medium"
        ),
    ];

    public static readonly IReadOnlyList<FieldDefinition> ImageText =
    [
        Image("imageUrl", "Image"),
        Text("imageAlt", "Image alt text"),
        Text("headline", "Headline"),
        RichText("content", "Content"),
        Dropdown("imagePosition", "Image position", Choices(("left", "Left"), ("right", "Right")), "left"),
        Text("buttonText", "Button text"),
        Text("buttonUrl", "Button URL"),
        Dropdown("buttonStyle", "Button style", ButtonStyles, "primary"),
        Color("backgroundColor", "Background color"),
    ];

    public static readonly IReadOnlyList<FieldDefinition> Card =
    [
        Text("title", "Title"),
        RichText("description", "Description"),
        Image("imageUrl", "Image"),
        Text("imageAlt", "Image alt text"),
        Text("buttonText", "Button text"),
        Text("buttonUrl", "Button URL"),
        Dropdown("buttonStyle", "Button style", ButtonStyles, "primary"),
        Color("backgroundColor", "Background color"),
    ];

    public static readonly IReadOnlyList<FieldDefinition> Faq =
    [
        Text("headline", "Headline"),
        Text("subheadline", "Subheadline"),
        Checkbox("expandFirst", "Expand first item", true),
        Color("backgroundColor", "Background color"),
        new()
        {
            DeveloperName = "items",
            Label = "Questions",
            FieldType = WidgetFieldType.Repeater,
            SubFields = [Text("question", "Question"), RichText("answer", "Answer")],
        },
    ];

    public static readonly IReadOnlyList<FieldDefinition> Cta =
    [
        Text("headline", "Headline"),
        RichText("content", "Content"),
        Text("buttonText", "Button text"),
        Text("buttonUrl", "Button URL"),
        Dropdown("buttonStyle", "Button style", ButtonStyles, "light"),
        Color("backgroundColor", "Background color", "#0d6efd"),
        Color("textColor", "Text color", "#ffffff"),
        Dropdown("alignment", "Alignment", Alignments, "center"),
    ];

    public static readonly IReadOnlyList<FieldDefinition> Embed =
    [
        Dropdown("embedType", "Embed type", Choices(("iframe", "Iframe"), ("html", "HTML")), "iframe"),
        Text("iframeUrl", "Iframe URL"),
        new()
        {
            DeveloperName = "htmlContent",
            Label = "Embed HTML",
            FieldType = WidgetFieldType.LongText,
            Description = "Rendered as raw HTML, scripts included.",
        },
        Dropdown(
            "aspectRatio",
            "Aspect ratio",
            Choices(("16x9", "16:9"), ("4x3", "4:3"), ("1x1", "1:1"), ("21x9", "21:9")),
            "16x9"
        ),
        Number("maxWidth", "Max width", null, "In pixels. Leave empty for full width."),
        Text("caption", "Caption"),
        Color("backgroundColor", "Background color"),
    ];

    public static readonly IReadOnlyList<FieldDefinition> ContentList =
    [
        Text("headline", "Headline"),
        Text("subheadline", "Subheadline"),
        new()
        {
            DeveloperName = "contentType",
            Label = "Content type",
            FieldType = WidgetFieldType.ContentType,
            IsRequired = true,
        },
        new()
        {
            DeveloperName = "viewId",
            Label = "View",
            FieldType = WidgetFieldType.View,
            ContentTypeField = "contentType",
            Description = "Leave empty for the default view.",
        },
        Text("filter", "Filter", "IsPublished eq 'true'", "OData filter expression."),
        Text("orderBy", "Order by", null, "OData order by expression. Overrides the view's sort."),
        Number("pageSize", "Page size", 3),
        Dropdown(
            "displayStyle",
            "Display style",
            Choices(("cards", "Cards"), ("list", "List"), ("compact", "Compact")),
            "cards"
        ),
        Checkbox("showImage", "Show image", true),
        Checkbox("showDate", "Show date", true),
        Checkbox("showExcerpt", "Show excerpt", true),
        Text("linkText", "View all text"),
        Text("linkUrl", "View all URL"),
        Color("backgroundColor", "Background color"),
    ];

    private static FieldDefinition Text(
        string name,
        string label,
        string? defaultValue = null,
        string? description = null
    ) => Field(name, label, WidgetFieldType.SingleLineText, defaultValue, description);

    private static FieldDefinition RichText(string name, string label) =>
        Field(name, label, WidgetFieldType.Wysiwyg, null, null);

    private static FieldDefinition Image(string name, string label) =>
        Field(name, label, WidgetFieldType.Image, null, null);

    private static FieldDefinition Color(string name, string label, string? defaultValue = null) =>
        Field(name, label, WidgetFieldType.Color, defaultValue, null);

    private static FieldDefinition Number(string name, string label, int? defaultValue, string? description = null) =>
        Field(name, label, WidgetFieldType.Number, defaultValue, description);

    private static FieldDefinition Checkbox(string name, string label, bool defaultValue) =>
        Field(name, label, WidgetFieldType.Checkbox, defaultValue, null);

    private static FieldDefinition Dropdown(
        string name,
        string label,
        ContentTypeFieldChoice[] choices,
        string defaultValue
    ) => Field(name, label, WidgetFieldType.Dropdown, defaultValue, null) with { Choices = choices };

    private static FieldDefinition Field(
        string name,
        string label,
        string fieldType,
        object? defaultValue,
        string? description
    ) =>
        new()
        {
            DeveloperName = name,
            Label = label,
            FieldType = fieldType,
            Description = description,
            DefaultValue = defaultValue is null ? null : JsonSerializer.SerializeToElement(defaultValue),
        };

    private static ContentTypeFieldChoice[] Choices(params (string DeveloperName, string Label)[] choices) =>
        choices.Select(c => new ContentTypeFieldChoice { DeveloperName = c.DeveloperName, Label = c.Label }).ToArray();
}
