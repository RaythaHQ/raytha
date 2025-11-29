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

    /// <summary>
    /// Revision history for this template.
    /// </summary>
    public virtual ICollection<WidgetTemplateRevision> Revisions { get; set; } =
        new List<WidgetTemplateRevision>();
}

