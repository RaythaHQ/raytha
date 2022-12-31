using Raytha.Domain.ValueObjects.FieldValues;

namespace Raytha.Domain.ValueObjects.FieldTypes;

public class DateFieldType : BaseFieldType
{
    public DateFieldType() : base("Date", "date", false) { }

    public override IEnumerable<ConditionOperator> SupportedConditionOperators
    {
        get
        {
            yield return ConditionOperator.EQUALS;
            yield return ConditionOperator.NOT_EQUALS;
            yield return ConditionOperator.GREATER_THAN;
            yield return ConditionOperator.GREATER_THAN_OR_EQUAL;
            yield return ConditionOperator.LESS_THAN;
            yield return ConditionOperator.LESS_THAN_OR_EQUAL;
            yield return ConditionOperator.IS_EMPTY;
            yield return ConditionOperator.IS_NOT_EMPTY;
        }
    }

    public override BaseFieldValue FieldValueFrom(dynamic value)
    {
        return new DateTimeFieldValue(value);
    }
}
