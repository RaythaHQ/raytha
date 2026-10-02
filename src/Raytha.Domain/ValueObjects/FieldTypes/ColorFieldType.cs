using Raytha.Domain.ValueObjects.FieldValues;

namespace Raytha.Domain.ValueObjects.FieldTypes;

public class ColorFieldType : BaseFieldType
{
    public ColorFieldType()
        : base("Color", "color", false) { }

    public override IEnumerable<ConditionOperator> SupportedConditionOperators
    {
        get
        {
            yield return ConditionOperator.EQUALS;
            yield return ConditionOperator.NOT_EQUALS;
            yield return ConditionOperator.IS_EMPTY;
            yield return ConditionOperator.IS_NOT_EMPTY;
        }
    }

    public override BaseFieldValue FieldValueFrom(dynamic value)
    {
        return new ColorFieldValue(value);
    }
}
