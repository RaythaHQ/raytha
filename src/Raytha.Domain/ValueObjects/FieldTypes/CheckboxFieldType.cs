using Raytha.Domain.ValueObjects.FieldValues;

namespace Raytha.Domain.ValueObjects.FieldTypes;

public class CheckboxFieldType : BaseFieldType
{
    public CheckboxFieldType() : base("Checkbox", "checkbox", false) { }

    public override IEnumerable<ConditionOperator> SupportedConditionOperators
    {
        get
        {
            yield return ConditionOperator.IS_TRUE;
            yield return ConditionOperator.IS_FALSE;
        }
    }

    public override BaseFieldValue FieldValueFrom(dynamic value)
    {
        return new BooleanFieldValue(value);
    }
}
