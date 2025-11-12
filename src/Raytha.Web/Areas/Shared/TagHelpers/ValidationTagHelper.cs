#nullable enable
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Raytha.Web.Areas.Shared.TagHelpers;

/// <summary>
/// Tag helper for displaying validation CSS classes and error messages.
/// Replaces the legacy HtmlHelper pattern with a more modern Tag Helper approach.
/// Usage: &lt;input raytha-validation-for="PropertyName" /&gt;
/// </summary>
[HtmlTargetElement("input", Attributes = ValidationForAttributeName)]
[HtmlTargetElement("select", Attributes = ValidationForAttributeName)]
[HtmlTargetElement("textarea", Attributes = ValidationForAttributeName)]
public class ValidationCssTagHelper : TagHelper
{
    private const string ValidationForAttributeName = "raytha-validation-for";

    /// <summary>
    /// Gets or sets the ViewContext for accessing validation state.
    /// </summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext? ViewContext { get; set; }

    /// <summary>
    /// Gets or sets the property name to check for validation errors.
    /// </summary>
    [HtmlAttributeName(ValidationForAttributeName)]
    public string? PropertyName { get; set; }

    /// <summary>
    /// Processes the tag by adding validation CSS classes if errors exist.
    /// </summary>
    /// <param name="context">The tag helper context.</param>
    /// <param name="output">The tag helper output.</param>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (ViewContext == null || string.IsNullOrWhiteSpace(PropertyName))
        {
            return;
        }

        // Check if ViewData contains ValidationFailures dictionary
        if (
            ViewContext.ViewData.TryGetValue("ValidationFailures", out var validationFailuresObj)
            && validationFailuresObj is Dictionary<string, string> validationFailures
        )
        {
            if (validationFailures.ContainsKey(PropertyName))
            {
                var existingClass = output.Attributes.FirstOrDefault(a => a.Name == "class");
                if (existingClass != null)
                {
                    output.Attributes.SetAttribute("class", $"{existingClass.Value} is-invalid");
                }
                else
                {
                    output.Attributes.SetAttribute("class", "is-invalid");
                }
            }
        }

        // Remove the custom attribute so it doesn't appear in the rendered HTML
        output.Attributes.RemoveAll(ValidationForAttributeName);
    }
}

/// <summary>
/// Tag helper for displaying validation error messages for a specific property.
/// Usage: &lt;div raytha-validation-message-for="PropertyName"&gt;&lt;/div&gt;
/// </summary>
[HtmlTargetElement("div", Attributes = ValidationMessageForAttributeName)]
[HtmlTargetElement("span", Attributes = ValidationMessageForAttributeName)]
public class ValidationMessageTagHelper : TagHelper
{
    private const string ValidationMessageForAttributeName = "raytha-validation-message-for";

    /// <summary>
    /// Gets or sets the ViewContext for accessing validation state.
    /// </summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext? ViewContext { get; set; }

    /// <summary>
    /// Gets or sets the property name to display error messages for.
    /// </summary>
    [HtmlAttributeName(ValidationMessageForAttributeName)]
    public string? PropertyName { get; set; }

    /// <summary>
    /// Processes the tag by adding the error message content if errors exist.
    /// </summary>
    /// <param name="context">The tag helper context.</param>
    /// <param name="output">The tag helper output.</param>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (ViewContext == null || string.IsNullOrWhiteSpace(PropertyName))
        {
            return;
        }

        // Check if ViewData contains ValidationFailures dictionary
        if (
            ViewContext.ViewData.TryGetValue("ValidationFailures", out var validationFailuresObj)
            && validationFailuresObj is Dictionary<string, string> validationFailures
            && validationFailures.TryGetValue(PropertyName, out var errorMessage)
        )
        {
            output.Content.SetContent(errorMessage);
        }

        // Remove the custom attribute so it doesn't appear in the rendered HTML
        output.Attributes.RemoveAll(ValidationMessageForAttributeName);
    }
}
