using Raytha.Application.Common.Utils;

namespace Raytha.Application.Common.Interfaces;

/// <summary>
/// Parses a Liquid template without rendering it. Saving a template that does not parse only
/// failed on the next public request, with an empty response.
/// </summary>
public interface ILiquidTemplateParser
{
    /// <summary>Null when <paramref name="source"/> parses. Otherwise the parser error and its position.</summary>
    LiquidSyntaxError? GetSyntaxError(string? source);
}
