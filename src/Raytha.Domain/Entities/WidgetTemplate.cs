using System.ComponentModel.DataAnnotations.Schema;

namespace Raytha.Domain.Entities;

/// <summary>
/// Stores the Liquid template for rendering a widget type.
/// Each theme has its own set of widget templates (one per widget type).
/// Similar to WebTemplate but for widgets.
/// </summary>
public class WidgetTemplate : BaseAuditableEntity
{
    /// <summary>
    /// Foreign key to the Theme this template belongs to.
    /// </summary>
    public required Guid ThemeId { get; set; }

    /// <summary>
    /// Navigation property to the Theme.
    /// </summary>
    public virtual Theme? Theme { get; set; }

    /// <summary>
    /// Display label shown in admin UI.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Developer name / widget type identifier (e.g., "hero", "wysiwyg").
    /// Maps to BuiltInWidgetType.DeveloperName.
    /// </summary>
    public string? DeveloperName { get; set; }

    /// <summary>
    /// The Liquid template content for rendering this widget type.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Whether this is a built-in widget template (all widget templates are built-in in V1).
    /// </summary>
    public bool IsBuiltInTemplate { get; set; } = true;

    public string _FieldsJson { get; set; } = "[]";

    /// <summary>
    /// The settings form for widgets of this type, in display order. Each field's DeveloperName is
    /// the key it writes in the widget's SettingsJson and reads as widget.settings.&lt;name&gt; in Liquid.
    /// </summary>
    [NotMapped]
    public IReadOnlyList<FieldDefinition> Fields
    {
        get => FieldDefinition.ListFromJson(_FieldsJson);
        set => _FieldsJson = FieldDefinition.ListToJson(value);
    }

    /// <summary>
    /// Revision history for this template.
    /// </summary>
    public virtual ICollection<WidgetTemplateRevision> Revisions { get; set; } =
        new List<WidgetTemplateRevision>();
}

