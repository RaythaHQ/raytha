using Raytha.Domain.ValueObjects.FieldValues;

namespace Raytha.Domain.ValueObjects.FieldTypes;

public class IdentifierFieldType : EqualsOrNotEqualsFieldType
{
    public IdentifierFieldType()
        : base("Id", "id", false) { }

    public override BaseFieldValue FieldValueFrom(dynamic value)
    {
        return new GuidFieldValue(value);
    }
}
