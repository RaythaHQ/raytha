using System.Text.Json;

namespace Raytha.Domain.ValueObjects.FieldValues;

public record ArrayFieldValue : BaseFieldValue
{
    private string[] _value = null;

    public ArrayFieldValue(object value)
    {
        if (value != null && !string.IsNullOrEmpty(value.ToString()))
        {
            if (value is string[])
            {
                _value = (string[])value;
            }
            else
            {
                try
                {
                    _value = JsonSerializer.Deserialize<string[]>(value.ToString());
                }
                catch { }
            }
        }
    }

    public override dynamic Value => _value;
    public override string Text => ToString();

    public override bool HasValue => _value != null && _value.Length > 0;

    public static implicit operator ArrayFieldValue(string[] s)
    {
        return new ArrayFieldValue(s);
    }

    public static implicit operator string[](ArrayFieldValue p)
    {
        return p.Value;
    }

    public override string ToString()
    {
        return _value != null ? string.Join(", ", _value) : string.Empty;
    }
}