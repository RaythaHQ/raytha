using Raytha.Domain.ValueObjects.FieldValues;
using System.ComponentModel.DataAnnotations.Schema;

namespace Raytha.Domain.ValueObjects.FieldTypes;

public abstract class BaseFieldType : ValueObject
{
    static BaseFieldType()
    {
    }

    protected BaseFieldType()
    {
    }

    protected BaseFieldType(string label, string developerName, bool hasChoices)
    {
        Label = label;
        DeveloperName = developerName;
        HasChoices = hasChoices;
    }

    public static BaseFieldType From(string developerName)
    {
        if (developerName.ToLower() == Id.DeveloperName)
            return Id;

        var type = SupportedTypes.FirstOrDefault(p => p.DeveloperName == developerName.ToLower());

        if (type == null)
        {
            throw new UnsupportedFieldTypeException(developerName);
        }

        return type;
    }

    public static BaseFieldType SingleLineText => new SingleLineTextFieldType();
    public static BaseFieldType LongText => new LongTextFieldType();
    public static BaseFieldType Wysiwyg => new WysiwygFieldType();
    public static BaseFieldType Radio => new RadioFieldType();
    public static BaseFieldType Dropdown => new DropdownFieldType();
    public static BaseFieldType Checkbox => new CheckboxFieldType();
    public static BaseFieldType MultipleSelect => new MultipleSelectFieldType();
    public static BaseFieldType Date => new DateFieldType();
    public static BaseFieldType Number => new NumberFieldType();
    public static BaseFieldType Attachment => new AttachmentFieldType();
    public static BaseFieldType OneToOneRelationship => new OneToOneRelationshipFieldType();
    public static BaseFieldType Id => new IdentifierFieldType();

    public string DeveloperName { get; private set; } = string.Empty;

    [NotMapped]
    public string Label { get; private set; } = string.Empty;

    [NotMapped]
    public bool HasChoices { get; private set; } = false;

    public static implicit operator string(BaseFieldType scheme)
    {
        return scheme.DeveloperName;
    }

    public static explicit operator BaseFieldType(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return Label;
    }

    public static IEnumerable<BaseFieldType> SupportedTypes
    {
        get
        {
            yield return SingleLineText;
            yield return LongText;
            yield return Wysiwyg;
            yield return Radio;
            yield return Dropdown;
            yield return Checkbox;
            yield return MultipleSelect;
            yield return Date;
            yield return Number;
            yield return Attachment;
            yield return OneToOneRelationship;
        }
    }

    [NotMapped]
    public abstract IEnumerable<ConditionOperator> SupportedConditionOperators { get; }

    public abstract BaseFieldValue FieldValueFrom(dynamic value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}

public abstract class EqualsOrNotEqualsFieldType : BaseFieldType
{
    public EqualsOrNotEqualsFieldType(string label, string developerName, bool hasChoices) : base(label, developerName, hasChoices) { }

    public override IEnumerable<ConditionOperator> SupportedConditionOperators
    {
        get
        {
            yield return ConditionOperator.EQUALS;
            yield return ConditionOperator.NOT_EQUALS;
        }
    }
}

public abstract class TextFieldType : BaseFieldType
{
    public TextFieldType(string label, string developerName, bool hasChoices) : base(label, developerName, hasChoices) { }

    public override IEnumerable<ConditionOperator> SupportedConditionOperators
    {
        get
        {
            yield return ConditionOperator.EQUALS;
            yield return ConditionOperator.NOT_EQUALS;
            yield return ConditionOperator.CONTAINS;
            yield return ConditionOperator.NOT_CONTAINS;
            yield return ConditionOperator.STARTS_WITH;
            yield return ConditionOperator.NOT_STARTS_WITH;
            yield return ConditionOperator.ENDS_WITH;
            yield return ConditionOperator.NOT_ENDS_WITH;
            yield return ConditionOperator.IS_EMPTY;
            yield return ConditionOperator.IS_NOT_EMPTY;
        }
    }
}

public abstract class SingleSelectFieldType : BaseFieldType
{
    public SingleSelectFieldType(string label, string developerName, bool hasChoices) : base(label, developerName, hasChoices) { }

    public override IEnumerable<ConditionOperator> SupportedConditionOperators
    {
        get
        {
            yield return ConditionOperator.EQUALS;
            yield return ConditionOperator.NOT_EQUALS;
            yield return ConditionOperator.IS_EMPTY;
            yield return ConditionOperator.IS_NOT_EMPTY;
        }
    }
}

public abstract class NumericValueFieldType : BaseFieldType
{
    public NumericValueFieldType(string label, string developerName, bool hasChoices) : base(label, developerName, hasChoices) { }

    public override IEnumerable<ConditionOperator> SupportedConditionOperators
    {
        get
        {
            yield return ConditionOperator.EQUALS;
            yield return ConditionOperator.NOT_EQUALS;
            yield return ConditionOperator.GREATER_THAN;
            yield return ConditionOperator.GREATER_THAN_OR_EQUAL;
            yield return ConditionOperator.LESS_THAN;
            yield return ConditionOperator.LESS_THAN_OR_EQUAL;
            yield return ConditionOperator.IS_EMPTY;
            yield return ConditionOperator.IS_NOT_EMPTY;

        }
    }
}



