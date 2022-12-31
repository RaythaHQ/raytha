using Raytha.Domain.ValueObjects.FieldValues;

namespace Raytha.Domain.ValueObjects.FieldTypes;

public class SingleLineTextFieldType : TextFieldType
{
    public SingleLineTextFieldType() : base("Single line text", "single_line_text", false) { }

    public override BaseFieldValue FieldValueFrom(dynamic value)
    {
        return new StringFieldValue(value);
    }
}
