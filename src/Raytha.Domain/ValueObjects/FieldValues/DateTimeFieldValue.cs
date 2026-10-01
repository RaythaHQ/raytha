namespace Raytha.Domain.ValueObjects.FieldValues;

public record DateTimeFieldValue : BaseFieldValue
{
    private DateTime? _value = null;
    private string? _unparsedText = null;

    public DateTimeFieldValue(object value)
    {
        if (value != null && !string.IsNullOrEmpty(value.ToString()))
            _value = Convert.ToDateTime(value.ToString());
    }

    /// <summary>
    /// A stored value that is not a date (a 1.x value the 2.0 upgrade could not convert) reads as
    /// empty and keeps its text, so it can still be seen and corrected.
    /// </summary>
    public static DateTimeFieldValue FromStored(object value)
    {
        try
        {
            return new DateTimeFieldValue(value);
        }
        catch (FormatException)
        {
            return new DateTimeFieldValue(string.Empty) { _unparsedText = value.ToString() };
        }
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
        return _value.HasValue ? _value.Value.ToString() : _unparsedText ?? string.Empty;
    }
}
