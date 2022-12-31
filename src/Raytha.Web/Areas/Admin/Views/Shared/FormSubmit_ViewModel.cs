using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;

namespace Raytha.Web.Areas.Admin.Views.Shared;

public interface IFormValidation
{
    IEnumerable<ValidationFailure> ValidationFailures { get; set; }
}

public class FormSubmit_ViewModel : IFormValidation
{
    public IEnumerable<ValidationFailure> ValidationFailures { get; set; }

    public string HasError(string propertyName)
    {
        return ValidationFailures != null && ValidationFailures.Any(p => p.PropertyName == propertyName) ? "is-invalid" : string.Empty;
    }

    public string ErrorMessageFor(string propertyName)
    {
        return ValidationFailures?.FirstOrDefault(p => p.PropertyName == propertyName)?.ErrorMessage;
    }
}
