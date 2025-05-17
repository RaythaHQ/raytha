namespace Raytha.Domain.ValueObjects;

public class VerificationCodeType : ValueObject
{
    static VerificationCodeType() { }

    private VerificationCodeType() { }

    private VerificationCodeType(string label, string developerName)
    {
        Label = label;
        DeveloperName = developerName;
    }

    public static VerificationCodeType From(string developerName)
    {
        var type = SupportedTypes.FirstOrDefault(p => p.DeveloperName == developerName);

        if (type == null)
        {
            throw new UnsupportedVerificationCodeTypeException(developerName);
        }

        return type;
    }

    public static VerificationCodeType ForgotPassword => new("Forgot password", "forgot_password");
    public static VerificationCodeType ResetEmail => new("Reset email", "reset_email");
    public static VerificationCodeType VerifyEmail => new("Verify email", "verify_email");
    public string Label { get; private set; } = string.Empty;
    public string DeveloperName { get; private set; } = string.Empty;

    public static implicit operator string(VerificationCodeType scheme)
    {
        return scheme.DeveloperName;
    }

    public static explicit operator VerificationCodeType(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return Label;
    }

    public static IEnumerable<VerificationCodeType> SupportedTypes
    {
        get
        {
            yield return ForgotPassword;
            yield return ResetEmail;
            yield return VerifyEmail;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}
