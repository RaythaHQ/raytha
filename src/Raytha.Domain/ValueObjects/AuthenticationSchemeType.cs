namespace Raytha.Domain.ValueObjects;

public class AuthenticationSchemeType : ValueObject
{
    static AuthenticationSchemeType() { }

    private AuthenticationSchemeType() { }

    private AuthenticationSchemeType(string label, string developerName)
    {
        Label = label;
        DeveloperName = developerName;
    }

    public static AuthenticationSchemeType From(string developerName)
    {
        var type = SupportedTypes.FirstOrDefault(p => p.DeveloperName == developerName);

        if (type == null)
        {
            throw new UnsupportedAuthenticationSchemeTypeException(developerName);
        }

        return type;
    }

    public static AuthenticationSchemeType MagicLink => new("Magic link", "magic_link");
    public static AuthenticationSchemeType EmailAndPassword =>
        new("Email and password", "email_and_password");
    public static AuthenticationSchemeType Jwt => new("Json web token", "jwt");
    public static AuthenticationSchemeType Saml => new("SAML", "saml");

    public string Label { get; private set; } = string.Empty;
    public string DeveloperName { get; private set; } = string.Empty;

    public static implicit operator string(AuthenticationSchemeType scheme)
    {
        return scheme.DeveloperName;
    }

    public static explicit operator AuthenticationSchemeType(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return Label;
    }

    public static IEnumerable<AuthenticationSchemeType> SupportedTypes
    {
        get
        {
            yield return EmailAndPassword;
            yield return MagicLink;
            yield return Jwt;
            yield return Saml;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}
