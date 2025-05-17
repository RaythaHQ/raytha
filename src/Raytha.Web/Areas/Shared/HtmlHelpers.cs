using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Raytha.Web.Areas.Shared;

public static class HtmlHelpers
{
    public static IHtmlContent HasError(
        this IHtmlHelper html,
        IEnumerable<ValidationFailure> errors,
        string propertyName
    )
    {
        if (errors != null && errors.Any(p => p.PropertyName == propertyName))
            return new HtmlString("is-invalid");

        return new HtmlString("");
    }

    public static IHtmlContent ErrorMessageFor(
        this IHtmlHelper html,
        IEnumerable<ValidationFailure> errors,
        string propertyName
    )
    {
        if (errors != null && errors.Any(p => p.PropertyName == propertyName))
            return new HtmlString(
                errors.Where(p => p.PropertyName == propertyName).First().ErrorMessage
            );

        return new HtmlString("");
    }
}
