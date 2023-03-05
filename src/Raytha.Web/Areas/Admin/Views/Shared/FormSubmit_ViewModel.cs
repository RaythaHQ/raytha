using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;

namespace Raytha.Web.Areas.Admin.Views.Shared;

public interface IFormValidation
{
    Dictionary<string, string> ValidationFailures { get; set; }
}

public class FormSubmit_ViewModel : IFormValidation
{
    public Dictionary<string, string> ValidationFailures { get; set; }

    public string HasError(string propertyName)
    {
        return ValidationFailures != null && ValidationFailures.Any(p => p.Key == propertyName) ? "is-invalid" : string.Empty;
    }

    public string ErrorMessageFor(string propertyName)
    {
        return ValidationFailures?.FirstOrDefault(p => p.Key == propertyName).Value;
    }
}
