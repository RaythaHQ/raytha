using Fluid;
using Fluid.Values;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using System;
using System.Threading.Tasks;

namespace Raytha.Web.Services;

public class RenderEngine : IRenderEngine
{
    private static readonly FluidParser _parser = new FluidParser(new FluidParserOptions { AllowFunctions = true });
    private readonly IRelativeUrlBuilder _relativeUrlBuilder;

    public RenderEngine(IRelativeUrlBuilder relativeUrlBuilder)
    {
        _relativeUrlBuilder = relativeUrlBuilder;
    }

    public string RenderAsHtml(string source, object entity)
    {
        if (_parser.TryParse(source, out var template, out var error))
        {
            var options = new TemplateOptions();
            options.MemberAccessStrategy = new UnsafeMemberAccessStrategy();
            options.TimeZone = DateTimeExtensions.GetTimeZoneInfo(DateTimeExtensions.DEFAULT_TIMEZONE);
            options.Filters.AddFilter("raytha_attachment_url", RaythaAttachmentUrl);
            
            var context = new TemplateContext(entity, options);
            string renderedHtml = template.Render(context);
            return renderedHtml;
        }
        else
        {
            throw new Exception(error);
        }
    }

    ValueTask<FluidValue> RaythaAttachmentUrl(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        return new StringValue(_relativeUrlBuilder.MediaRedirectToFileUrl(input.ToStringValue()));
    }
}
