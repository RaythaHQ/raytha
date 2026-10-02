namespace Raytha.Domain.Exceptions;

public class FilterConditionTypeNotFoundException : Exception
{
    public FilterConditionTypeNotFoundException(string developerName)
        : base(
            $"Filter condition type \"{developerName}\" is unknown. Valid types are filter_condition and filter_condition_group."
        ) { }
}
