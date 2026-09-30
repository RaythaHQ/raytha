using Raytha.Domain.Common;
using Raytha.Domain.Entities;
using Raytha.Domain.Exceptions;

namespace Raytha.Domain.ValueObjects;

/// <summary>
/// The widget templates every theme starts with: their default Liquid and default fields.
/// Themes may add custom widget templates; those have no entry here.
/// </summary>
public class BuiltInWidgetType : ValueObject
{
    static BuiltInWidgetType() { }

    private BuiltInWidgetType() { }

    private BuiltInWidgetType(
        string displayName,
        string developerName,
        string description,
        string iconClass,
        IReadOnlyList<FieldDefinition> fields
    )
    {
        DisplayName = displayName;
        DeveloperName = developerName;
        Description = description;
        IconClass = iconClass;
        Fields = fields;
    }

    public static BuiltInWidgetType From(string developerName)
    {
        var type = WidgetTypes.FirstOrDefault(p => p.DeveloperName == developerName);

        if (type == null)
        {
            throw new UnsupportedWidgetTypeException(developerName);
        }

        return type;
    }

    public static BuiltInWidgetType? Find(string? developerName) =>
        WidgetTypes.FirstOrDefault(p => p.DeveloperName == developerName);

    public static bool IsBuiltIn(string? developerName) => Find(developerName) != null;

    public static BuiltInWidgetType Hero =>
        new(
            "Hero",
            "hero",
            "Large banner with headline, subtext, and optional call-to-action button.",
            "bi-card-heading",
            BuiltInWidgetFields.Hero
        );

    public static BuiltInWidgetType Wysiwyg =>
        new(
            "WYSIWYG",
            "wysiwyg",
            "Rich text content block with WYSIWYG editor.",
            "bi-file-earmark-richtext",
            BuiltInWidgetFields.Wysiwyg
        );

    public static BuiltInWidgetType ImageText =>
        new(
            "Image + Text",
            "imagetext",
            "Image alongside text content with configurable layout.",
            "bi-image",
            BuiltInWidgetFields.ImageText
        );

    public static BuiltInWidgetType Card =>
        new(
            "Card",
            "card",
            "A single card with image, title, description, and call-to-action.",
            "bi-card-text",
            BuiltInWidgetFields.Card
        );

    public static BuiltInWidgetType FAQ =>
        new(
            "FAQ",
            "faq",
            "Expandable accordion of frequently asked questions.",
            "bi-question-circle",
            BuiltInWidgetFields.Faq
        );

    public static BuiltInWidgetType CTA =>
        new(
            "Call to Action",
            "cta",
            "Prominent call-to-action block with heading and button.",
            "bi-megaphone",
            BuiltInWidgetFields.Cta
        );

    public static BuiltInWidgetType Embed =>
        new(
            "Embed",
            "embed",
            "Embed external content via iframe or raw HTML.",
            "bi-code-slash",
            BuiltInWidgetFields.Embed
        );

    public static BuiltInWidgetType ContentList =>
        new(
            "Content List",
            "contentlist",
            "Displays a list of content items from a content type.",
            "bi-list-ul",
            BuiltInWidgetFields.ContentList
        );

    /// <summary>
    /// Display name shown in admin UI.
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Developer name used as identifier and in template paths.
    /// </summary>
    public string DeveloperName { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Bootstrap Icons class shown in the admin widget picker.
    /// </summary>
    public string IconClass { get; private set; } = string.Empty;

    public IReadOnlyList<FieldDefinition> Fields { get; private set; } = [];

    /// <summary>
    /// Gets the default Liquid template content for this widget type.
    /// Template file is located at Entities/DefaultTemplates/raytha_widget_{developerName}.liquid
    /// </summary>
    public string DefaultTemplateContent
    {
        get
        {
            var pathToFile = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Entities",
                "DefaultTemplates",
                $"raytha_widget_{DeveloperName}.liquid"
            );
            return File.ReadAllText(pathToFile);
        }
    }

    public WidgetTemplate CreateTemplate(Guid themeId)
    {
        var template = new WidgetTemplate { Id = Guid.NewGuid(), ThemeId = themeId };
        ResetTemplate(template);
        return template;
    }

    public void ResetTemplate(WidgetTemplate template)
    {
        template.Label = DisplayName;
        template.DeveloperName = DeveloperName;
        template.Content = DefaultTemplateContent;
        template.IsBuiltInTemplate = true;
        template.Fields = Fields;
    }

    public static implicit operator string(BuiltInWidgetType type)
    {
        return type.DeveloperName;
    }

    public static explicit operator BuiltInWidgetType(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return DeveloperName;
    }

    /// <summary>
    /// All available built-in widget types.
    /// </summary>
    public static IEnumerable<BuiltInWidgetType> WidgetTypes
    {
        get
        {
            yield return Hero;
            yield return Wysiwyg;
            yield return ImageText;
            yield return Card;
            yield return FAQ;
            yield return CTA;
            yield return Embed;
            yield return ContentList;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}
