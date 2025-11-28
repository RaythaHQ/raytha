using Raytha.Domain.Common;
using Raytha.Domain.Exceptions;

namespace Raytha.Domain.ValueObjects;

/// <summary>
/// Defines the built-in widget types available for Site Pages.
/// Each widget type has a system name, display name, and default Liquid template.
/// </summary>
public class BuiltInWidgetType : ValueObject
{
    static BuiltInWidgetType() { }

    private BuiltInWidgetType() { }

    private BuiltInWidgetType(string displayName, string developerName)
    {
        DisplayName = displayName;
        DeveloperName = developerName;
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

    /// <summary>
    /// Hero widget - large banner with headline, subtext, and optional CTA button.
    /// </summary>
    public static BuiltInWidgetType Hero => new("Hero", "hero");

    /// <summary>
    /// WYSIWYG widget - Rich text content block.
    /// </summary>
    public static BuiltInWidgetType Wysiwyg => new("WYSIWYG", "wysiwyg");

    /// <summary>
    /// Image + Text widget - image alongside text content, configurable layout.
    /// </summary>
    public static BuiltInWidgetType ImageText => new("Image + Text", "imagetext");

    /// <summary>
    /// Card widget - single card with image, title, description, and CTA.
    /// </summary>
    public static BuiltInWidgetType Card => new("Card", "card");

    /// <summary>
    /// FAQ widget - expandable accordion of questions and answers.
    /// </summary>
    public static BuiltInWidgetType FAQ => new("FAQ", "faq");

    /// <summary>
    /// Call to Action widget - prominent CTA block with heading, text, and button.
    /// </summary>
    public static BuiltInWidgetType CTA => new("Call to Action", "cta");

    /// <summary>
    /// Embed widget - embed external content via iframe or script.
    /// </summary>
    public static BuiltInWidgetType Embed => new("Embed", "embed");

    /// <summary>
    /// Content List widget - displays a list of content items from a content type.
    /// </summary>
    public static BuiltInWidgetType ContentList => new("Content List", "contentlist");

    /// <summary>
    /// Display name shown in admin UI.
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Developer name used as identifier and in template paths.
    /// </summary>
    public string DeveloperName { get; private set; } = string.Empty;

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

    /// <summary>
    /// Gets the template developer name for database storage.
    /// </summary>
    public string TemplateDeveloperName => $"raytha_widget_{DeveloperName}";

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
