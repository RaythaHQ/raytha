using CSharpVitamins;

namespace Raytha.Domain.ValueObjects.FieldValues;

public record GuidFieldValue : BaseFieldValue
{
    private Guid _value;

    public GuidFieldValue(object value)
    {
        var text = value?.ToString();
        if (string.IsNullOrEmpty(text))
            return;
        if (Guid.TryParse(text, out _value))
            return;
        if (ShortGuid.TryParse(text, out ShortGuid shortGuid))
            _value = shortGuid.Guid;
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
