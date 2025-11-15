namespace Raytha.Domain.ValueObjects;

public class FilterConditionType : ValueObject
{
    static FilterConditionType() { }

    public FilterConditionType() { }

    private FilterConditionType(string developerName)
    {
        DeveloperName = developerName;
    }

    public static FilterConditionType From(string developerName)
    {
        var type = SupportedTypes.FirstOrDefault(p => p.DeveloperName == developerName.ToLower());

        if (type == null)
        {
            throw new FilterConditionTypeNotFoundException(developerName);
        }

        return type;
    }

    public static FilterConditionType FilterCondition => new("filter_condition");
    public static FilterConditionType FilterConditionGroup => new("filter_condition_group");
    public string DeveloperName { get; set; } = string.Empty;

    public static implicit operator string(FilterConditionType scheme)
    {
        return scheme.DeveloperName;
    }

    public static explicit operator FilterConditionType(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return DeveloperName;
    }

    public static IEnumerable<FilterConditionType> SupportedTypes
    {
        get
        {
            yield return FilterCondition;
            yield return FilterConditionGroup;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}
