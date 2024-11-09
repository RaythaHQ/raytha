using Raytha.Domain.ValueObjects.FieldValues;

namespace Raytha.Domain.ValueObjects.FieldTypes;

public class NumberFieldType : NumericValueFieldType
{
    public const string NUMBER_LABEL = "Number";
    public const string NUMBER_DEVELOPER_NAME = "number";
    public NumberFieldType() : base(NUMBER_LABEL, NUMBER_DEVELOPER_NAME, false) { }

    public override BaseFieldValue FieldValueFrom(dynamic value)
    {
        return new DecimalFieldValue(value);
    }
}
