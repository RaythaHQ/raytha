using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Domain.ValueObjects.FieldValues;

public class AttachmentFieldType : BaseFieldType
{
    public AttachmentFieldType() : base("Attachment", "attachment", false) { }

    public override IEnumerable<ConditionOperator> SupportedConditionOperators
    {
        get
        {
            yield return ConditionOperator.IS_EMPTY;
            yield return ConditionOperator.IS_NOT_EMPTY;
        }
    }

    public override BaseFieldValue FieldValueFrom(dynamic value)
    {
        return new StringFieldValue(value);
    }
}