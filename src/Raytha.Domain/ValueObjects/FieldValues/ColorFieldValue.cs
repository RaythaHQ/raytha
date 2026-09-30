using System.Text.RegularExpressions;

namespace Raytha.Domain.ValueObjects.FieldValues;

/// <summary>
/// A <c>#rrggbb</c> color in lowercase. <c>#rgb</c> and upper case are accepted and
/// normalized; anything else throws <see cref="FormatException"/>.
/// </summary>
public partial record ColorFieldValue : BaseFieldValue
{
    private readonly string? _value;

    public ColorFieldValue(object? value)
    {
        var text = value?.ToString()?.Trim();
        if (string.IsNullOrEmpty(text))
            return;

        var match = HexColor().Match(text);
        if (!match.Success)
            throw new FormatException($"'{text}' is not a #rrggbb color.");

        var hex = match.Groups[1].Value.ToLowerInvariant();
        _value = hex.Length == 3 ? $"#{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}" : $"#{hex}";
    }

    public override dynamic Value => _value;
    public override string Text => ToString();
    public override bool HasValue => !string.IsNullOrEmpty(_value);

    public override string ToString() => _value ?? string.Empty;

    [GeneratedRegex("^#([0-9a-fA-F]{6}|[0-9a-fA-F]{3})$")]
    private static partial Regex HexColor();
}
