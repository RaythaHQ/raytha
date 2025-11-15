using Raytha.Domain.ValueObjects.FieldValues;

namespace Raytha.Domain.ValueObjects.FieldTypes;

public class LongTextFieldType : TextFieldType
{
    public LongTextFieldType()
        : base("Long text", "long_text", false) { }

    public override BaseFieldValue FieldValueFrom(dynamic value)
    {
        return new StringFieldValue(value);
    }
}
