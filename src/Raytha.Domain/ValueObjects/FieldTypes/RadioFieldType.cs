using Raytha.Domain.ValueObjects.FieldValues;

namespace Raytha.Domain.ValueObjects.FieldTypes;

public class RadioFieldType : SingleSelectFieldType
{
    public RadioFieldType()
        : base("Radio", "radio", true) { }

    public override BaseFieldValue FieldValueFrom(dynamic value)
    {
        return new StringFieldValue(value);
    }
}
