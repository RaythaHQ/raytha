using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Raytha.Web.Areas.Shared;

/// <summary>
/// Legacy HTML helper methods for validation display.
/// NOTE: Consider migrating to the ValidationTagHelper for new code.
/// These are maintained for backward compatibility with existing views.
/// </summary>
public static class HtmlHelpers
{
    /// <summary>
    /// Returns "is-invalid" CSS class if the specified property has validation errors.
    /// </summary>
    /// <param name="html">The HTML helper instance.</param>
    /// <param name="errors">Collection of validation failures.</param>
    /// <param name="propertyName">The property name to check for errors.</param>
    /// <returns>HTML string containing "is-invalid" or empty string.</returns>
    public static IHtmlContent HasError(
        this IHtmlHelper html,
        IEnumerable<ValidationFailure>? errors,
        string propertyName
    )
    {
        if (
            errors != null
            && !string.IsNullOrWhiteSpace(propertyName)
            && errors.Any(p => p.PropertyName == propertyName)
        )
        {
            return new HtmlString("is-invalid");
        }

        return new HtmlString(string.Empty);
    }

    /// <summary>
    /// Returns the error message for the specified property if validation errors exist.
    /// </summary>
    /// <param name="html">The HTML helper instance.</param>
    /// <param name="errors">Collection of validation failures.</param>
    /// <param name="propertyName">The property name to retrieve error message for.</param>
    /// <returns>HTML string containing the error message or empty string.</returns>
    public static IHtmlContent ErrorMessageFor(
        this IHtmlHelper html,
        IEnumerable<ValidationFailure>? errors,
        string propertyName
    )
    {
        if (errors != null && !string.IsNullOrWhiteSpace(propertyName))
        {
            var error = errors.FirstOrDefault(p => p.PropertyName == propertyName);
            if (error != null)
            {
                return new HtmlString(error.ErrorMessage ?? string.Empty);
            }
        }

        return new HtmlString(string.Empty);
    }
}
