namespace Raytha.Domain.ValueObjects;

public class ConditionOperator : ValueObject
{
    static ConditionOperator()
    {
    }

    public ConditionOperator()
    {
    }

    private ConditionOperator(string label, string developerName, bool hasFieldValue)
    {
        DeveloperName = developerName;
        Label = label;
        HasFieldValue = hasFieldValue;
    }

    public static ConditionOperator From(string developerName)
    {
        var type = SupportedOperators.FirstOrDefault(p => p.DeveloperName == developerName.ToLower());

        if (type == null)
        {
            throw new UnsupportedConditionOperatorException(developerName);
        }

        return type;
    }

    public static ConditionOperator LESS_THAN => new("<", "lt", true);
    public static ConditionOperator LESS_THAN_OR_EQUAL => new("≤", "le", true);
    public static ConditionOperator GREATER_THAN => new(">", "gt", true);
    public static ConditionOperator GREATER_THAN_OR_EQUAL => new("≥", "ge", true);
    public static ConditionOperator EQUALS => new("=", "eq", true);
    public static ConditionOperator NOT_EQUALS => new("≠", "ne", true);
    public static ConditionOperator CONTAINS => new("contains", "contains", true);
    public static ConditionOperator NOT_CONTAINS => new("does not contain", "notcontains", true);
    public static ConditionOperator STARTS_WITH => new("starts with", "startswith", true);
    public static ConditionOperator NOT_STARTS_WITH => new("does not start with", "notstartswith", true);
    public static ConditionOperator ENDS_WITH => new("ends with", "endswith", true);
    public static ConditionOperator NOT_ENDS_WITH => new("does not end with", "notendswith", true);
    public static ConditionOperator HAS => new("has", "has", true);
    public static ConditionOperator NOT_HAS => new("does not have", "nothas", true);
    public static ConditionOperator IS_TRUE => new("is true", "true", false);
    public static ConditionOperator IS_FALSE => new("is false", "false", false);
    public static ConditionOperator IS_EMPTY => new("is empty", "empty", false);
    public static ConditionOperator IS_NOT_EMPTY => new("is not empty", "notempty", false);
    public string Label { get; set; } = string.Empty;
    public string DeveloperName { get; set; } = string.Empty;
    public bool HasFieldValue { get; set; }

    public static implicit operator string(ConditionOperator scheme)
    {
        return scheme.DeveloperName;
    }

    public static explicit operator ConditionOperator(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return DeveloperName;
    }

    public static IEnumerable<ConditionOperator> SupportedOperators
    {
        get
        {
            yield return EQUALS;
            yield return NOT_EQUALS;
            yield return LESS_THAN;
            yield return LESS_THAN_OR_EQUAL;
            yield return GREATER_THAN;
            yield return GREATER_THAN_OR_EQUAL;
            yield return CONTAINS;
            yield return NOT_CONTAINS;
            yield return STARTS_WITH;
            yield return NOT_STARTS_WITH;
            yield return ENDS_WITH;
            yield return NOT_ENDS_WITH;
            yield return HAS;
            yield return NOT_HAS;
            yield return IS_TRUE;
            yield return IS_FALSE;
            yield return IS_EMPTY;
            yield return IS_NOT_EMPTY;
        }
    }

    public static IEnumerable<ConditionOperator> OperatorsWithoutValues
    {
        get
        {
            yield return IS_TRUE;
            yield return IS_FALSE;
            yield return IS_EMPTY;
            yield return IS_NOT_EMPTY;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}