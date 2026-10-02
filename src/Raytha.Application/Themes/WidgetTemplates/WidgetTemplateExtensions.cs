using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Themes.WidgetTemplates;

public static class WidgetTemplateExtensions
{
    /// <summary>The template's current label, content, and fields, saved before a change so it can be reverted.</summary>
    public static WidgetTemplateRevision ToRevision(this WidgetTemplate template) =>
        new()
        {
            Id = Guid.NewGuid(),
            WidgetTemplateId = template.Id,
            Label = template.Label,
            Content = template.Content,
            _FieldsJson = template._FieldsJson,
        };

    /// <summary>
    /// Revisions saved before widget templates had fields hold none; reverting to one keeps the
    /// template's current fields rather than emptying its settings form.
    /// </summary>
    public static void RestoreFrom(this WidgetTemplate template, WidgetTemplateRevision revision)
    {
        template.Label = revision.Label;
        template.Content = revision.Content;
        if (FieldDefinition.ListFromJson(revision._FieldsJson).Count > 0)
        {
            template._FieldsJson = revision._FieldsJson;
        }
    }

    public static WidgetTemplate CopyToTheme(this WidgetTemplate template, Guid themeId) =>
        new()
        {
            Id = Guid.NewGuid(),
            ThemeId = themeId,
            Label = template.Label,
            DeveloperName = template.DeveloperName,
            Content = template.Content,
            IsBuiltInTemplate = template.IsBuiltInTemplate,
            _FieldsJson = template._FieldsJson,
        };

    /// <summary>
    /// Theme packages exported before widget templates had fields carry none; a built-in then gets
    /// its default fields so its settings form is not empty. Packaged fields get the same checks as
    /// fields saved in the admin, because widget settings validation trusts them.
    /// </summary>
    public static WidgetTemplate ToWidgetTemplate(this WidgetTemplateJson json, Guid themeId)
    {
        var fields = json.Fields is { Count: > 0 } packaged
            ? packaged
            : BuiltInWidgetType.Find(json.DeveloperName)?.Fields ?? [];

        var errors = WidgetFieldDefinitions.Validate(fields).ToList();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Widget template '{json.DeveloperName}' has invalid fields. {string.Join(" ", errors)}"
            );
        }

        return new()
        {
            Id = Guid.NewGuid(),
            ThemeId = themeId,
            DeveloperName = json.DeveloperName,
            Label = json.Label,
            Content = json.Content,
            IsBuiltInTemplate = json.IsBuiltInTemplate,
            Fields = fields,
        };
    }
}
