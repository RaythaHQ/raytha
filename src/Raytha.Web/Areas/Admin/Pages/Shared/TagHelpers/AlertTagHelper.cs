#nullable enable
using System.Text;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Raytha.Web.Areas.Admin.Pages.Shared.TagHelpers;

/// <summary>
/// Tag helper for rendering alert messages from ViewData/TempData.
/// Usage: &lt;alert /&gt; or &lt;alert alert-key="ErrorMessage" /&gt;
/// Renders Bootstrap alert divs for ErrorMessage, SuccessMessage, WarningMessage, and InfoMessage.
/// </summary>
[HtmlTargetElement("alert")]
public class AlertTagHelper : TagHelper
{
    /// <summary>
    /// Gets or sets the ViewContext for accessing ViewData.
    /// </summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    /// <summary>
    /// Gets or sets the specific alert key to render.
    /// If null, renders all alert types (ErrorMessage, SuccessMessage, WarningMessage, InfoMessage).
    /// </summary>
    [HtmlAttributeName("alert-key")]
    public string? AlertKey { get; set; }

    /// <summary>
    /// Gets or sets whether the alerts should be dismissible.
    /// </summary>
    [HtmlAttributeName("dismissible")]
    public bool Dismissible { get; set; } = true;

    /// <summary>
    /// Order for this tag helper.
    /// </summary>
    public override int Order => 100;

    /// <summary>
    /// Processes the tag by rendering alerts from ViewData.
    /// </summary>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null; // Remove the <alert> tag itself

        var keys = string.IsNullOrEmpty(AlertKey)
            ? new[] { "ErrorMessage", "SuccessMessage", "WarningMessage", "InfoMessage" }
            : new[] { AlertKey };

        var html = new StringBuilder();

        foreach (var key in keys)
        {
            if (ViewContext.ViewData[key] is string message && !string.IsNullOrWhiteSpace(message))
            {
                var alertType = GetAlertType(key);
                var dismissibleClass = Dismissible ? " alert-dismissible fade show" : "";

                html.AppendLine(
                    $"<div class=\"alert alert-{alertType}{dismissibleClass} mt-2\" role=\"alert\">"
                );
                html.AppendLine($"    {message}");

                if (Dismissible)
                {
                    html.AppendLine(
                        "    <button type=\"button\" class=\"btn-close\" data-bs-dismiss=\"alert\" aria-label=\"Close\"></button>"
                    );
                }

                html.AppendLine("</div>");
            }
        }

        if (html.Length > 0)
        {
            output.Content.SetHtmlContent(html.ToString());
        }
        else
        {
            output.SuppressOutput();
        }
    }

    /// <summary>
    /// Maps a ViewData key to a Bootstrap alert type.
    /// </summary>
    private static string GetAlertType(string key)
    {
        return key switch
        {
            "ErrorMessage" => "danger",
            "SuccessMessage" => "success",
            "WarningMessage" => "warning",
            "InfoMessage" => "info",
            _ => "info",
        };
    }
}
