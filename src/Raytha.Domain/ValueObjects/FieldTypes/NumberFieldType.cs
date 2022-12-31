using Raytha.Domain.ValueObjects.FieldValues;

namespace Raytha.Domain.ValueObjects.FieldTypes;

public class NumberFieldType : NumericValueFieldType
{
    public NumberFieldType() : base("Number", "number", false) { }

    public override BaseFieldValue FieldValueFrom(dynamic value)
    {
        return new DecimalFieldValue(value);
    }
}
