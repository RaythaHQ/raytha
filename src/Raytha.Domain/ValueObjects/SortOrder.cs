namespace Raytha.Domain.ValueObjects;

public class SortOrder : ValueObject
{
    public const string ASCENDING = "asc";
    public const string DESCENDING = "desc";

    static SortOrder() { }

    public SortOrder() { }

    private SortOrder(string label, string developerName)
    {
        Label = label;
        DeveloperName = developerName;
    }

    public static SortOrder From(string developerName)
    {
        var type = SupportedTypes.FirstOrDefault(p => p.DeveloperName == developerName.ToLower());

        if (type == null)
        {
            throw new SortOrderNotFoundException(developerName);
        }

        return type;
    }

    public static SortOrder Ascending => new("Ascending", ASCENDING);
    public static SortOrder Descending => new("Descending", DESCENDING);

    public string Label { get; set; } = string.Empty;
    public string DeveloperName { get; set; } = string.Empty;

    public static implicit operator string(SortOrder scheme)
    {
        return scheme.DeveloperName;
    }

    public static explicit operator SortOrder(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return Label;
    }

    public static IEnumerable<SortOrder> SupportedTypes
    {
        get
        {
            yield return Ascending;
            yield return Descending;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}
