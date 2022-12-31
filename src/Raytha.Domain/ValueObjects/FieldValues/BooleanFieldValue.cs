namespace Raytha.Domain.ValueObjects.FieldValues;

public record BooleanFieldValue : BaseFieldValue
{
    private bool? _value = null;

    public BooleanFieldValue(object value)
    {
        if (value != null && !string.IsNullOrEmpty(value.ToString()))
            _value = Convert.ToBoolean(value.ToString());
    }

    public override dynamic Value => _value;
    public override string Text => ToString();

    public override bool HasValue => _value.HasValue;

    public static implicit operator BooleanFieldValue(bool? s)
    {
        return new BooleanFieldValue(s);
    }

    public static implicit operator bool?(BooleanFieldValue p)
    {
        return p.Value;
    }

    public override string ToString()
    {
        return _value.HasValue ? _value.Value.ToString() : string.Empty;
    }
}