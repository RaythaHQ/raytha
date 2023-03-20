using Fluid;
using Fluid.Filters;
using Fluid.Values;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace Raytha.Web.Services;

public class RenderEngine : IRenderEngine
{
    private static readonly FluidParser _parser = new FluidParser(new FluidParserOptions { AllowFunctions = true });
    private readonly IRelativeUrlBuilder _relativeUrlBuilder;
    private readonly ICurrentOrganization _currentOrganization;

    public RenderEngine(IRelativeUrlBuilder relativeUrlBuilder, ICurrentOrganization currentOrganization)
    {
        _relativeUrlBuilder = relativeUrlBuilder;
        _currentOrganization = currentOrganization;
    }

    public string RenderAsHtml(string source, object entity)
    {
        if (_parser.TryParse(source, out var template, out var error))
        {
            var options = new TemplateOptions();
            options.MemberAccessStrategy = new UnsafeMemberAccessStrategy();
            options.TimeZone = DateTimeExtensions.GetTimeZoneInfo(_currentOrganization.TimeZone);
            options.Filters.AddFilter("raytha_attachment_url", RaythaAttachmentUrl);
            options.Filters.AddFilter("organization_time", LocalDateFilter);
            options.Filters.AddFilter("groupby", GroupBy);
            var context = new TemplateContext(entity, options);
            string renderedHtml = template.Render(context);
            return renderedHtml;
        }
        else
        {
            throw new Exception(error);
        }
    }

    public ValueTask<FluidValue> RaythaAttachmentUrl(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        return new StringValue(_relativeUrlBuilder.MediaRedirectToFileUrl(input.ToStringValue()));
    }

    public ValueTask<FluidValue> GroupBy(FluidValue input, FilterArguments property, TemplateContext context)
    {
        var groupByProperty = property.At(0).ToStringValue();
        var groups = input.Enumerate(context).GroupBy(p => ApplyGroupBy(p, groupByProperty, context));
        var result = new List<FluidValue>();
        foreach (var group in groups)
        {
            result.Add(new ObjectValue(new 
            {
                key = group.Key,
                items = group.ToList()
            }));
        }
        return new ArrayValue(result);
    }

    private string ApplyGroupBy(FluidValue p, string groupByProperty, TemplateContext context)
    {
        if (groupByProperty.StartsWith("PublishedContent") && groupByProperty.Contains("."))
        {
            var developerName = groupByProperty.Split(".").ElementAt(1);
            return p.GetValueAsync("PublishedContent", context).Result.GetValueAsync(developerName, context).Result.ToStringValue();
        }
        else
        {
            return p.GetValueAsync(groupByProperty, context).Result.ToStringValue();
        }
    }

    public ValueTask<FluidValue> LocalDateFilter(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        var value = TimeZoneConverter(input, context);
        return ReferenceEquals(value, NilValue.Instance) ? value : MiscFilters.Date(value, arguments, context);
    }

    private FluidValue TimeZoneConverter(FluidValue input, TemplateContext context)
    {
        if (!input.TryGetDateTimeInput(context, out var value))
        {
            return NilValue.Instance;
        }

        var utc = DateTime.SpecifyKind(value.DateTime, DateTimeKind.Utc);

        // Create new offset for UTC
        var localOffset = new DateTimeOffset(utc, TimeSpan.Zero);

        var result = TimeZoneInfo.ConvertTime(localOffset, context.TimeZone);
        return new DateTimeValue(result);
    }
}
