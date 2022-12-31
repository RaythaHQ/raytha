namespace Raytha.Web.Areas.Public.Views.Login;

public class LoginWithEmailAndPassword_ViewModel
{
    public string EmailAddress { get; set; }

    public string Password { get; set; }

    public bool RememberMe { get; set; } = false;
}

public class LoginWithMagicLink_ViewModel
{
    public string EmailAddress { get; set; }
}

public class BeginForgotPassword_ViewModel
{
    public string EmailAddress { get; set; }
}

public class CompleteForgotPassword_ViewModel
{
    public string Id { get; set; }

    public string NewPassword { get; set; }

    public string ConfirmNewPassword { get; set; }
}

public class CreateUser_ViewModel
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string EmailAddress { get; set; }
    public string Password { get; set; }
    public string ConfirmPassword { get; set; }
}

