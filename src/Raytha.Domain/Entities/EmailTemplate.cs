namespace Raytha.Domain.Entities;

public class EmailTemplate : BaseAuditableEntity
{
    public string? Subject { get; set; }
    public string? DeveloperName { get; set; }
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public string? Content { get; set; }
    public bool IsBuiltInTemplate { get; set; }
    public virtual ICollection<WebTemplateRevision> Revisions { get; set; } =
        new List<WebTemplateRevision>();
}

public class BuiltInEmailTemplate : ValueObject
{
    static BuiltInEmailTemplate() { }

    private BuiltInEmailTemplate() { }

    private BuiltInEmailTemplate(string subject, string developerName, bool safeToCc)
    {
        DefaultSubject = subject;
        DeveloperName = developerName;
        SafeToCc = safeToCc;
    }

    public static BuiltInEmailTemplate From(string developerName)
    {
        var type = Templates.FirstOrDefault(p => p.DeveloperName == developerName);

        if (type == null)
        {
            throw new UnsupportedTemplateTypeException(developerName);
        }

        return type;
    }

    public static BuiltInEmailTemplate AdminWelcomeEmail =>
        new(
            "[{{ CurrentOrganization.OrganizationName }}] An administrator has created your account",
            "raytha_email_admin_welcome",
            false
        );
    public static BuiltInEmailTemplate AdminPasswordChangedEmail =>
        new(
            "[{{ CurrentOrganization.OrganizationName }}] Your password has been changed",
            "raytha_email_admin_passwordchanged",
            false
        );
    public static BuiltInEmailTemplate AdminPasswordResetEmail =>
        new(
            "[{{ CurrentOrganization.OrganizationName }}] Your password has been reset by an administrator",
            "raytha_email_admin_passwordreset",
            false
        );

    public static BuiltInEmailTemplate LoginBeginLoginWithMagicLinkEmail =>
        new(
            "[{{ CurrentOrganization.OrganizationName }}] Website login access link",
            "raytha_email_login_beginloginwithmagiclink",
            false
        );
    public static BuiltInEmailTemplate LoginBeginForgotPasswordEmail =>
        new(
            "[{{ CurrentOrganization.OrganizationName }}] Password recovery",
            "raytha_email_login_beginforgotpassword",
            false
        );
    public static BuiltInEmailTemplate LoginCompletedForgotPasswordEmail =>
        new(
            "[{{ CurrentOrganization.OrganizationName }}] Your password has been recovered",
            "raytha_email_login_completedforgotpassword",
            false
        );

    public static BuiltInEmailTemplate UserWelcomeEmail =>
        new(
            "[{{ CurrentOrganization.OrganizationName }}] An administrator has created your account",
            "raytha_email_user_welcome",
            false
        );
    public static BuiltInEmailTemplate UserPasswordChangedEmail =>
        new(
            "[{{ CurrentOrganization.OrganizationName }}] Your password has been changed",
            "raytha_email_user_passwordchanged",
            false
        );
    public static BuiltInEmailTemplate UserPasswordResetEmail =>
        new(
            "[{{ CurrentOrganization.OrganizationName }}] Your password has been reset by an administrator",
            "raytha_email_user_passwordreset",
            false
        );

    public string DefaultSubject { get; private set; } = string.Empty;
    public string DeveloperName { get; private set; } = string.Empty;
    public bool SafeToCc { get; private set; } = false;

    public string DefaultContent
    {
        get
        {
            var pathToFile = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Entities",
                "DefaultTemplates",
                $"{DeveloperName}.liquid"
            );
            return File.ReadAllText(pathToFile);
        }
    }

    public static implicit operator string(BuiltInEmailTemplate scheme)
    {
        return scheme.DeveloperName;
    }

    public static explicit operator BuiltInEmailTemplate(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return DeveloperName;
    }

    public static IEnumerable<BuiltInEmailTemplate> Templates
    {
        get
        {
            yield return AdminWelcomeEmail;
            yield return AdminPasswordChangedEmail;
            yield return AdminPasswordResetEmail;

            yield return LoginBeginLoginWithMagicLinkEmail;
            yield return LoginBeginForgotPasswordEmail;
            yield return LoginCompletedForgotPasswordEmail;

            yield return UserWelcomeEmail;
            yield return UserPasswordChangedEmail;
            yield return UserPasswordResetEmail;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}
