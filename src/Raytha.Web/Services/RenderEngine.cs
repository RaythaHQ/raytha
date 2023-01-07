using Fluid;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using System;

namespace Raytha.Web.Services;

public class RenderEngine : IRenderEngine
{
    private static readonly FluidParser _parser = new FluidParser(new FluidParserOptions { AllowFunctions = true });

    public string RenderAsHtml(string source, object entity)
    {
        if (_parser.TryParse(source, out var template, out var error))
        {
            var options = new TemplateOptions();
            options.MemberAccessStrategy = new UnsafeMemberAccessStrategy();
            options.TimeZone = DateTimeExtensions.GetTimeZoneInfo(DateTimeExtensions.DEFAULT_TIMEZONE);
            var context = new TemplateContext(entity, options);
            string renderedHtml = template.Render(context);
            return renderedHtml;
        }
        else
        {
            throw new Exception(error);
        }
    }
}
