namespace Raytha.Domain.ValueObjects.FieldValues;

public record StringFieldValue : BaseFieldValue
{
    private readonly string _value = null;

    public StringFieldValue(object value)
    {
        if (value != null && !string.IsNullOrEmpty(value.ToString()))
            _value = value.ToString();
    }

    public override dynamic Value => _value;
    public override string Text => ToString();
    public override bool HasValue => !string.IsNullOrEmpty(_value);

    public static implicit operator StringFieldValue(string s)
    {
        return new StringFieldValue(s);
    }

    public static implicit operator string(StringFieldValue p)
    {
        return p.Value;
    }

    public override string ToString()
    {
        return string.IsNullOrEmpty(_value) ? string.Empty : _value;
    }
}
