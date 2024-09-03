using Fluid;
using Fluid.Filters;
using Fluid.Values;
using MediatR;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using Raytha.Application.ContentItems.Queries;
using Raytha.Application.ContentTypes.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Raytha.Application.NavigationMenuItems;
using Raytha.Application.NavigationMenuItems.Queries;
using Raytha.Application.NavigationMenus;
using Raytha.Application.NavigationMenus.Queries;

namespace Raytha.Web.Services;

public class RenderEngine : IRenderEngine
{
    private static readonly FluidParser _parser = new FluidParser(new FluidParserOptions { AllowFunctions = true });
    private readonly IRelativeUrlBuilder _relativeUrlBuilder;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly IMediator _mediator;
    private readonly IFileStorageProvider _fileStorageProvider;

    public RenderEngine(
        IMediator mediator, 
        IRelativeUrlBuilder relativeUrlBuilder, 
        ICurrentOrganization currentOrganization, 
        IFileStorageProvider fileStorageProvider)
    {
        _relativeUrlBuilder = relativeUrlBuilder;
        _currentOrganization = currentOrganization;
        _mediator = mediator;
        _fileStorageProvider = fileStorageProvider;
    }

    public string RenderAsHtml(string source, object entity)
    {
        if (_parser.TryParse(source, out var template, out var error))
        {
            var options = new TemplateOptions();
            options.MemberAccessStrategy = new UnsafeMemberAccessStrategy();
            options.TimeZone = DateTimeExtensions.GetTimeZoneInfo(_currentOrganization.TimeZone);
            options.Filters.AddFilter("attachment_redirect_url", AttachmentRedirectUrl);
            options.Filters.AddFilter("attachment_public_url", AttachmentPublicUrl);
            options.Filters.AddFilter("organization_time", LocalDateFilter);
            options.Filters.AddFilter("groupby", GroupBy);
            options.Filters.AddFilter("json", JsonFilter);

            var context = new TemplateContext(entity, options);
            context.SetValue("get_content_item_by_id", GetContentItemById());
            context.SetValue("get_content_items", GetContentItems());
            context.SetValue("get_content_type_by_developer_name", GetContentTypeByDeveloperName());
            context.SetValue("get_main_menu", GetMainMenu());
            context.SetValue("get_menu", GetMenuByDeveloperName());
            string renderedHtml = template.Render(context);
            return renderedHtml;
        }
        else
        {
            throw new Exception(error);
        }
    }

    public ValueTask<FluidValue> AttachmentRedirectUrl(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        return new StringValue(_relativeUrlBuilder.MediaRedirectToFileUrl(input.ToStringValue()));
    }

    public ValueTask<FluidValue> AttachmentPublicUrl(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        return new StringValue(_fileStorageProvider.GetDownloadUrlAsync(input.ToStringValue(), FileStorageUtility.GetDefaultExpiry()).Result);
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

    public FunctionValue GetContentItemById()
    {
        return new FunctionValue(async (args, context) => 
        {
            var contentItemId = args.At(0).ToStringValue();
            var result = await _mediator.Send(new GetContentItemById.Query { Id = contentItemId });
            return new ObjectValue(result.Result);
        });
    }

    public FunctionValue GetContentItems()
    {
        return new FunctionValue(async (args, context) => 
        {
            var contentType = args["ContentType"].ToStringValue();
            var filter = args["Filter"].ToStringValue();
            var orderBy = args["OrderBy"].ToStringValue();
            var pageNumber = args["PageNumber"].ToNumberValue();
            var pageSize = args["PageSize"].ToNumberValue();
            var result = await _mediator.Send(new GetContentItems.Query 
            { 
                ContentType = contentType,
                Filter = filter,
                OrderBy = orderBy,
                PageNumber = (int)pageNumber,
                PageSize = (int)pageSize
            });
            return new ObjectValue(result.Result);
        });
    }

    public FunctionValue GetContentTypeByDeveloperName()
    {
        return new FunctionValue(async (args, context) =>
        {
            var developerName = args.At(0).ToStringValue();
            var result = await _mediator.Send(new GetContentTypeByDeveloperName.Query { DeveloperName = developerName });
            return new ObjectValue(result.Result);
        });
    }

    public FunctionValue GetMainMenu()
    {
        return new FunctionValue(async (_, _) =>
        {
            var mainMenuResponse = await _mediator.Send(new GetMainMenu.Query());
            var menuItemsResponse = await _mediator.Send(new GetNavigationMenuItemsByNavigationMenuId.Query
            {
                NavigationMenuId = mainMenuResponse.Result.Id,
            });

            var menuItems = menuItemsResponse.Result.BuildTree<NavigationMenuItem_RenderModel>(NavigationMenuItem_RenderModel.GetProjection);
            var mainMenu = NavigationMenu_RenderModel.GetProjection(mainMenuResponse.Result, menuItems);

            return new ObjectValue(mainMenu);
        });
    }

    public FunctionValue GetMenuByDeveloperName()
    {
        return new FunctionValue(async (args, _) =>
        {
            var developerName = args.At(0).ToStringValue();
            var menuResponse = await _mediator.Send(new GetNavigationMenuByDeveloperName.Query
            {
                DeveloperName = developerName
            });

            var menuItemsResponse = await _mediator.Send(new GetNavigationMenuItemsByNavigationMenuId.Query
            {
                NavigationMenuId = menuResponse.Result.Id,
            });

            var menuItems = menuItemsResponse.Result.BuildTree<NavigationMenuItem_RenderModel>(NavigationMenuItem_RenderModel.GetProjection);
            var menu = NavigationMenu_RenderModel.GetProjection(menuResponse.Result, menuItems);

            return new ObjectValue(menu);
        });
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
    
    public static ValueTask<FluidValue> JsonFilter(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        return new StringValue(JsonSerializer.Serialize(input.ToObjectValue()));
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
