using Raytha.Domain.ValueObjects.FieldValues;

namespace Raytha.Domain.ValueObjects.FieldTypes;

public class OneToOneRelationshipFieldType : TextFieldType
{
    public OneToOneRelationshipFieldType() : base("One to one relationship", "one_to_one_relationship", false) { }

    public override BaseFieldValue FieldValueFrom(dynamic value)
    {
        return new GuidFieldValue(value);
    }
}
