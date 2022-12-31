using Raytha.Domain.ValueObjects.FieldValues;

namespace Raytha.Domain.ValueObjects.FieldTypes;

public class DropdownFieldType : SingleSelectFieldType
{
    public DropdownFieldType() : base("Dropdown", "dropdown", true) { }

    public override BaseFieldValue FieldValueFrom(dynamic value)
    {
        return new StringFieldValue(value);
    }
}