namespace Raytha.Domain.ValueObjects;

public class BooleanOperator : ValueObject
{
    static BooleanOperator()
    {
    }

    public BooleanOperator()
    {
    }

    private BooleanOperator(string developerName)
    {
        DeveloperName = developerName.ToLower();
    }

    public static BooleanOperator From(string developerName)
    {
        var type = SupportedOperators.FirstOrDefault(p => p.DeveloperName == developerName.ToLower());

        if (type == null)
        {
            throw new UnsupportedBooleanOperatorException(developerName);
        }

        return type;
    }

    public static BooleanOperator AND => new("AND");
    public static BooleanOperator OR => new("OR");
    public static BooleanOperator NOT => new("NOT");
    public string DeveloperName { get; set; } = string.Empty;

    public static implicit operator string(BooleanOperator scheme)
    {
        return scheme.DeveloperName;
    }

    public static explicit operator BooleanOperator(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return DeveloperName;
    }

    public static IEnumerable<BooleanOperator> SupportedOperators
    {
        get
        {
            yield return AND;
            yield return OR;
            yield return NOT;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}