using Fluid;
using Raytha.Application.Common.Interfaces;

namespace Raytha.Web.Services;

public class LiquidTemplateParser : ILiquidTemplateParser
{
    private static readonly FluidParser Parser = new(new FluidParserOptions { AllowFunctions = true });

    public string? GetSyntaxError(string? source)
    {
        return Parser.TryParse(source ?? string.Empty, out _, out var error) ? null : error;
    }
}
