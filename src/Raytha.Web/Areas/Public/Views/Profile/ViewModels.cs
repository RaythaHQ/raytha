namespace Raytha.Web.Areas.Public.Views.Profile;

public class ChangePasswordViewModel : FormSubmitViewModel
{
    public string CurrentPassword { get; set; }

    public string NewPassword { get; set; }

    public string ConfirmNewPassword { get; set; }
}

public class ChangeProfileViewModel : FormSubmitViewModel
{
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string EmailAddress { get; set; }
}

public interface IFormValidation
{
    Dictionary<string, string> ValidationFailures { get; set; }
}

public class FormSubmitViewModel : IFormValidation
{
    public Dictionary<string, string> ValidationFailures { get; set; }

    public string HasError(string propertyName)
    {
        return ValidationFailures != null && ValidationFailures.Any(p => p.Key == propertyName)
            ? "is-invalid"
            : string.Empty;
    }

    public string ErrorMessageFor(string propertyName)
    {
        return ValidationFailures?.FirstOrDefault(p => p.Key == propertyName).Value;
    }
}
