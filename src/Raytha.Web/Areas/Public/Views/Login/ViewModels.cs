namespace Raytha.Web.Areas.Public.Views.Login;

public class LoginWithEmailAndPasswordViewModel
{
    public string EmailAddress { get; set; }

    public string Password { get; set; }

    public bool RememberMe { get; set; } = false;
}

public class LoginWithMagicLinkViewModel
{
    public string EmailAddress { get; set; }
}

public class BeginForgotPasswordViewModel
{
    public string EmailAddress { get; set; }
}

public class CompleteForgotPasswordViewModel
{
    public string Id { get; set; }

    public string NewPassword { get; set; }

    public string ConfirmNewPassword { get; set; }
}

public class CreateUserViewModel
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string EmailAddress { get; set; }
    public string Password { get; set; }
    public string ConfirmPassword { get; set; }
}
