namespace Raytha.Domain.ValueObjects.FieldValues;

public record DateTimeFieldValue : BaseFieldValue
{
    private DateTime? _value = null;

    public DateTimeFieldValue(object value)
    {
        if (value != null && !string.IsNullOrEmpty(value.ToString()))
            _value = Convert.ToDateTime(value.ToString());
    }

    public override dynamic Value => _value;
    public override string Text => ToString();

    public override bool HasValue => _value.HasValue && _value != DateTime.MinValue;

    public static implicit operator DateTimeFieldValue(DateTime? s)
    {
        return new DateTimeFieldValue(s);
    }

    public static implicit operator DateTime?(DateTimeFieldValue p)
    {
        return p.Value;
    }

    public override string ToString()
    {
        return _value.HasValue ? _value.Value.ToString() : string.Empty;
    }
}