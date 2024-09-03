namespace Raytha.Domain.Entities;

public class WebTemplate : BaseAuditableEntity
{
    public required Guid ThemeId { get; set; }
    public virtual Theme? Theme { get; set; }
    public bool IsBaseLayout { get; set; } = false;
    public string? Label { get; set; }
    public string? DeveloperName { get; set; }
    public string? Content { get; set; }
    public bool IsBuiltInTemplate { get; set; }
    public Guid? ParentTemplateId { get; set; }
    public bool AllowAccessForNewContentTypes { get; set; }
    public virtual WebTemplate? ParentTemplate { get; set; }
    public virtual ICollection<WebTemplateRevision> Revisions { get; set; } = new List<WebTemplateRevision>();
    public virtual ICollection<WebTemplateAccessToModelDefinition> TemplateAccessToModelDefinitions { get; set; } = new List<WebTemplateAccessToModelDefinition>();    
}

public class BuiltInWebTemplate : ValueObject
{
    static BuiltInWebTemplate()
    {
    }

    private BuiltInWebTemplate()
    {
    }

    private BuiltInWebTemplate(string label, string developerName, bool isBuiltInTemplate)
    {
        DefaultLabel = label;
        DeveloperName = developerName;
        IsBuiltInTemplate = isBuiltInTemplate;
    }

    public static BuiltInWebTemplate From(string developerName)
    {
        var type = Templates.FirstOrDefault(p => p.DeveloperName == developerName);

        if (type == null)
        {
            throw new UnsupportedTemplateTypeException(developerName);
        }

        return type;
    }

    public static BuiltInWebTemplate _Layout => new("_Layout", "raytha_html_base_layout", true);
    public static BuiltInWebTemplate _LoginLayout => new("_LoginLayout", "raytha_html_base_login_layout", true);
    public static BuiltInWebTemplate Error403 => new("Error, access denied", "raytha_html_error_403", true);
    public static BuiltInWebTemplate Error404 => new("Error, resource not found", "raytha_html_error_404", true);
    public static BuiltInWebTemplate Error500 => new("Error, something broke", "raytha_html_error_500", true);
    public static BuiltInWebTemplate HomePage => new("Home", "raytha_html_home", false);
    public static BuiltInWebTemplate ContentItemListViewPage => new("Content item list view", "raytha_html_content_item_list", false);
    public static BuiltInWebTemplate ContentItemDetailViewPage => new("Content item detail view", "raytha_html_content_item_detail", false);
    public static BuiltInWebTemplate LoginWithEmailAndPasswordPage => new("Login with email and password", "raytha_html_login_emailandpassword", true);
    public static BuiltInWebTemplate LoginWithMagicLinkPage => new("Login with magic link", "raytha_html_login_magiclink", true);
    public static BuiltInWebTemplate LoginWithMagicLinkSentPage => new("Magic link sent", "raytha_html_login_magiclinksent", true);
    public static BuiltInWebTemplate UserRegistrationForm => new("User registration form", "raytha_html_user_registration", true);
    public static BuiltInWebTemplate UserRegistrationFormSuccess => new("User registration form success", "raytha_html_user_registration_success", true);
    public static BuiltInWebTemplate ChangeProfilePage => new("Change profile", "raytha_html_changeprofile", true);
    public static BuiltInWebTemplate ChangePasswordPage => new("Change password", "raytha_html_changepassword", true);
    public static BuiltInWebTemplate ForgotPasswordPage => new("Forgot password", "raytha_html_forgotpassword", true);
    public static BuiltInWebTemplate ForgotPasswordCompletePage => new("Complete forgot password", "raytha_html_forgotpasswordcomplete", true);
    public static BuiltInWebTemplate ForgotPasswordResetLinkSentPage => new("Forgot password reset link sent", "raytha_html_forgotpassword_reset_link_sent", true);
    public static BuiltInWebTemplate ForgotPasswordSuccessPage => new("Forgot password success", "raytha_html_forgotpasswordsuccess", true);

    public string DefaultLabel { get; private set; } = string.Empty;
    public string DeveloperName { get; private set; } = string.Empty;
    public bool IsBuiltInTemplate { get; private set; } = true;

    public string DefaultContent
    {
        get
        {
            var pathToFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Entities", "DefaultTemplates", $"{DeveloperName}.liquid");
            return File.ReadAllText(pathToFile);
        }
    }

    public static implicit operator string(BuiltInWebTemplate scheme)
    {
        return scheme.DeveloperName;
    }

    public static explicit operator BuiltInWebTemplate(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return DeveloperName;
    }

    public static IEnumerable<BuiltInWebTemplate> Templates
    {
        get
        {
            yield return _Layout;
            yield return _LoginLayout;
            yield return Error403;
            yield return Error404;
            yield return Error500;
            yield return HomePage;
            yield return ContentItemListViewPage;
            yield return ContentItemDetailViewPage;
            yield return UserRegistrationForm;
            yield return UserRegistrationFormSuccess;
            yield return ChangeProfilePage;
            yield return ChangePasswordPage;
            yield return LoginWithEmailAndPasswordPage;
            yield return LoginWithMagicLinkPage;
            yield return LoginWithMagicLinkSentPage;
            yield return ForgotPasswordPage;
            yield return ForgotPasswordCompletePage;
            yield return ForgotPasswordResetLinkSentPage;
            yield return ForgotPasswordSuccessPage;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}
