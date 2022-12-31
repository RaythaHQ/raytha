namespace Raytha.Domain.ValueObjects.FieldValues;

public record GuidFieldValue : BaseFieldValue
{
    private Guid _value;

    public GuidFieldValue(object value)
    {
        if (value != null && !string.IsNullOrEmpty(value.ToString()))
            Guid.TryParse(value.ToString(), out _value);
    }

    public override dynamic Value => _value;
    public override string Text => ToString();

    public override bool HasValue => _value != Guid.Empty;

    public static implicit operator GuidFieldValue(Guid? s)
    {
        return new GuidFieldValue(s);
    }

    public static implicit operator Guid(GuidFieldValue p)
    {
        return p.Value;
    }

    public override string ToString()
    {
        return _value != Guid.Empty ? _value.ToString() : string.Empty;
    }
}
