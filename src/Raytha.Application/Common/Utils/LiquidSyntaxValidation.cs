using FluentValidation;
using Raytha.Application.Common.Interfaces;

namespace Raytha.Application.Common.Utils;

public static class LiquidSyntaxValidation
{
    public static void RejectInvalidLiquid<T>(
        ValidationContext<T> context,
        ILiquidTemplateParser parser,
        string? content
    )
    {
        var error = parser.GetSyntaxError(content);
        if (!string.IsNullOrEmpty(error))
            context.AddFailure("Content", error);
    }
}
