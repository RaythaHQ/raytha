using Raytha.Domain.ValueObjects.FieldValues;

namespace Raytha.Domain.ValueObjects.FieldTypes;

/// <summary>
/// Rows of sub-fields defined on <c>ContentTypeField.SubFields</c>. Only emptiness can be
/// queried; rows are never sorted or text-matched.
/// </summary>
public class RepeaterFieldType : BaseFieldType
{
    public RepeaterFieldType()
        : base("Repeater", "repeater", false) { }

    public static IReadOnlyList<string> SubFieldTypes { get; } =
    [
        SingleLineText.DeveloperName,
        LongText.DeveloperName,
        Wysiwyg.DeveloperName,
        Number.DeveloperName,
        Checkbox.DeveloperName,
        Date.DeveloperName,
        Dropdown.DeveloperName,
        Radio.DeveloperName,
        Color.DeveloperName,
        Attachment.DeveloperName,
    ];

    public override IEnumerable<ConditionOperator> SupportedConditionOperators
    {
        get
        {
            yield return ConditionOperator.IS_EMPTY;
            yield return ConditionOperator.IS_NOT_EMPTY;
        }
    }

    public override bool IsSortable => false;
    public override bool IsSearchable => false;
    public override bool StoresJsonArray => true;

    public override BaseFieldValue FieldValueFrom(dynamic value)
    {
        return new RepeaterFieldValue(value);
    }

}
