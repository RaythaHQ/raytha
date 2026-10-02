using System.Globalization;
using System.Text.RegularExpressions;

namespace Raytha.Application.Common.Utils;

/// <summary>
/// A Liquid parse failure. Fluid reports the position only inside the message, as <c>at (line:column)</c>.
/// </summary>
public sealed record LiquidSyntaxError(string Message, int? Line, int? Column)
{
    private static readonly Regex Location = new(
        @"at \((\d+):(\d+)\)",
        RegexOptions.CultureInvariant | RegexOptions.Compiled
    );

    public static LiquidSyntaxError? FromParserMessage(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return null;
        }

        var text = error.Trim();
        var match = Location.Match(text);
        if (!match.Success)
        {
            return new LiquidSyntaxError(text, null, null);
        }

        return new LiquidSyntaxError(
            text,
            int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture)
        );
    }

    public string Describe()
    {
        return Line is int line && Column is int column
            ? $"Line {line}, column {column}: {Message}"
            : Message;
    }
}
