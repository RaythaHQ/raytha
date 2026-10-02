using System.Text.RegularExpressions;
using Fluid;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;

namespace Raytha.Web.Services;

public class LiquidTemplateParser : ILiquidTemplateParser
{
    private static readonly FluidParser Parser = new(new FluidParserOptions { AllowFunctions = true });

    public LiquidSyntaxError? GetSyntaxError(string? source)
    {
        // {% renderbody %} is the child slot of a base layout. It is substituted as text before rendering
        // and is not a Fluid tag, so blank it (same length, so line and column stay true) before parsing.
        var parseable = Regex.Replace(
            source ?? string.Empty,
            WebTemplateExtensions.RENDERBODY_REGEX,
            m => new string(' ', m.Length),
            RegexOptions.IgnoreCase
        );
        return Parser.TryParse(parseable, out _, out var error)
            ? null
            : LiquidSyntaxError.FromParserMessage(error);
    }
}
