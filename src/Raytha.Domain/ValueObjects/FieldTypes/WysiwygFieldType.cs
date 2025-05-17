using Raytha.Domain.ValueObjects.FieldValues;

namespace Raytha.Domain.ValueObjects.FieldTypes;

public class WysiwygFieldType : TextFieldType
{
    public WysiwygFieldType()
        : base("Wysiwyg", "wysiwyg", false) { }

    public override BaseFieldValue FieldValueFrom(dynamic value)
    {
        return new StringFieldValue(value);
    }
}
