namespace Raytha.Domain.ValueObjects.FieldValues;

public record DecimalFieldValue : BaseFieldValue
{
    private readonly decimal? _value;

    public DecimalFieldValue(object value)
    {
        if (value != null && !string.IsNullOrEmpty(value.ToString()))
            _value = Convert.ToDecimal(value.ToString());
    }

    public override dynamic Value => _value;
    public override string Text => ToString();

    public override bool HasValue => _value.HasValue;

    public static implicit operator DecimalFieldValue(decimal? s)
    {
        return new DecimalFieldValue(s);
    }

    public static implicit operator decimal?(DecimalFieldValue p)
    {
        return p.Value;
    }

    public override string ToString()
    {
        return _value.HasValue ? _value.Value.ToString() : string.Empty;
    }
}