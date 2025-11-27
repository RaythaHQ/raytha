using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fluid;
using Fluid.Filters;
using Fluid.Values;
using Mediator;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using Raytha.Application.ContentItems.Queries;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Application.NavigationMenuItems;
using Raytha.Application.NavigationMenuItems.Queries;
using Raytha.Application.NavigationMenus;
using Raytha.Application.NavigationMenus.Queries;
using Raytha.Application.RaythaFunctions.Queries;
using Raytha.Domain.ValueObjects;

namespace Raytha.Web.Services;

public class RenderEngine : IRenderEngine
{
    private static readonly FluidParser _parser = new FluidParser(
        new FluidParserOptions { AllowFunctions = true }
    );

    private static readonly TemplateOptions _templateOptions;
    private static readonly ConcurrentDictionary<string, IFluidTemplate> _templateCache = new();
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly IRelativeUrlBuilder _relativeUrlBuilder;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly IMediator _mediator;
    private readonly IFileStorageProvider _fileStorageProvider;
    private readonly IRaythaFunctionScriptEngine _raythaFunctionScriptEngine;
    private readonly IRaythaFunctionConfiguration _raythaFunctionConfiguration;

    static RenderEngine()
    {
        _templateOptions = new TemplateOptions();
        _templateOptions.MemberAccessStrategy = new UnsafeMemberAccessStrategy();
        _templateOptions.Filters.AddFilter("attachment_redirect_url", AttachmentRedirectUrl);
        _templateOptions.Filters.AddFilter("attachment_public_url", AttachmentPublicUrl);
        _templateOptions.Filters.AddFilter("organization_time", LocalDateFilter);
        _templateOptions.Filters.AddFilter("groupby", GroupBy);
        _templateOptions.Filters.AddFilter("json", JsonFilter);
    }

    public RenderEngine(
        IMediator mediator,
        IRelativeUrlBuilder relativeUrlBuilder,
        ICurrentOrganization currentOrganization,
        IFileStorageProvider fileStorageProvider,
        IRaythaFunctionScriptEngine raythaFunctionScriptEngine,
        IRaythaFunctionConfiguration raythaFunctionConfiguration
    )
    {
        _relativeUrlBuilder = relativeUrlBuilder;
        _currentOrganization = currentOrganization;
        _mediator = mediator;
        _fileStorageProvider = fileStorageProvider;
        _raythaFunctionScriptEngine = raythaFunctionScriptEngine;
        _raythaFunctionConfiguration = raythaFunctionConfiguration;
    }

    public string RenderAsHtml(string source, object entity)
    {
        var template = _templateCache.GetOrAdd(
            source,
            key =>
            {
                if (_parser.TryParse(key, out var parsedTemplate, out var error))
                {
                    return parsedTemplate;
                }
                throw new Exception(error);
            }
        );

        var context = new TemplateContext(entity, _templateOptions);
        context.TimeZone = DateTimeExtensions.GetTimeZoneInfo(_currentOrganization.TimeZone);

        // Store services in ambient values for filters to access
        context.AmbientValues["RelativeUrlBuilder"] = _relativeUrlBuilder;
        context.AmbientValues["FileStorageProvider"] = _fileStorageProvider;

        context.SetValue("get_content_item_by_id", GetContentItemById());
        context.SetValue("get_content_items", GetContentItems());
        context.SetValue("get_content_type_by_developer_name", GetContentTypeByDeveloperName());
        context.SetValue("get_main_menu", GetMainMenu());
        context.SetValue("get_menu", GetMenuByDeveloperName());
        context.SetValue("raytha_function", RaythaFunction());

        return template.Render(context);
    }

    private static ValueTask<FluidValue> AttachmentRedirectUrl(
        FluidValue input,
        FilterArguments arguments,
        TemplateContext context
    )
    {
        var relativeUrlBuilder = (IRelativeUrlBuilder)context.AmbientValues["RelativeUrlBuilder"];
        return new StringValue(relativeUrlBuilder.MediaRedirectToFileUrl(input.ToStringValue()));
    }

    private static ValueTask<FluidValue> AttachmentPublicUrl(
        FluidValue input,
        FilterArguments arguments,
        TemplateContext context
    )
    {
        if (string.IsNullOrEmpty(input.ToStringValue()))
            return new StringValue(string.Empty);

        var fileStorageProvider = (IFileStorageProvider)
            context.AmbientValues["FileStorageProvider"];
        return new StringValue(
            fileStorageProvider
                .GetDownloadUrlAsync(input.ToStringValue(), FileStorageUtility.GetDefaultExpiry())
                .Result
        );
    }

    private static ValueTask<FluidValue> GroupBy(
        FluidValue input,
        FilterArguments property,
        TemplateContext context
    )
    {
        var groupByProperty = property.At(0).ToStringValue();
        var groups = input
            .Enumerate(context)
            .GroupBy(p => ApplyGroupBy(p, groupByProperty, context));
        var result = new List<FluidValue>();
        foreach (var group in groups)
        {
            result.Add(new ObjectValue(new { key = group.Key, items = group.ToList() }));
        }
        return new ArrayValue(result);
    }

    public FunctionValue GetContentItemById()
    {
        return new FunctionValue(
            async (args, context) =>
            {
                var contentItemId = args.At(0).ToStringValue();
                var result = await _mediator.Send(
                    new GetContentItemById.Query { Id = contentItemId }
                );
                return new ObjectValue(result.Result);
            }
        );
    }

    public FunctionValue GetContentItems()
    {
        return new FunctionValue(
            async (args, context) =>
            {
                var contentType = args["ContentType"].ToStringValue();
                var filter = args["Filter"].ToStringValue();
                var orderBy = args["OrderBy"].ToStringValue();
                var pageNumber = args["PageNumber"].ToNumberValue();
                var pageSize = args["PageSize"].ToNumberValue();
                var result = await _mediator.Send(
                    new GetContentItems.Query
                    {
                        ContentType = contentType,
                        Filter = filter,
                        OrderBy = orderBy,
                        PageNumber = (int)pageNumber,
                        PageSize = (int)pageSize,
                    }
                );
                return new ObjectValue(result.Result);
            }
        );
    }

    public FunctionValue GetContentTypeByDeveloperName()
    {
        return new FunctionValue(
            async (args, context) =>
            {
                var developerName = args.At(0).ToStringValue();
                var result = await _mediator.Send(
                    new GetContentTypeByDeveloperName.Query { DeveloperName = developerName }
                );
                return new ObjectValue(result.Result);
            }
        );
    }

    public FunctionValue GetMainMenu()
    {
        return new FunctionValue(
            async (_, _) =>
            {
                var mainMenuResponse = await _mediator.Send(new GetMainMenu.Query());
                var menuItemsResponse = await _mediator.Send(
                    new GetNavigationMenuItemsByNavigationMenuId.Query
                    {
                        NavigationMenuId = mainMenuResponse.Result.Id,
                    }
                );

                var menuItems = menuItemsResponse.Result.BuildTree<NavigationMenuItem_RenderModel>(
                    NavigationMenuItem_RenderModel.GetProjection
                );
                var mainMenu = NavigationMenu_RenderModel.GetProjection(
                    mainMenuResponse.Result,
                    menuItems
                );

                return new ObjectValue(mainMenu);
            }
        );
    }

    public FunctionValue GetMenuByDeveloperName()
    {
        return new FunctionValue(
            async (args, _) =>
            {
                var developerName = args.At(0).ToStringValue();
                var menuResponse = await _mediator.Send(
                    new GetNavigationMenuByDeveloperName.Query { DeveloperName = developerName }
                );

                var menuItemsResponse = await _mediator.Send(
                    new GetNavigationMenuItemsByNavigationMenuId.Query
                    {
                        NavigationMenuId = menuResponse.Result.Id,
                    }
                );

                var menuItems = menuItemsResponse.Result.BuildTree<NavigationMenuItem_RenderModel>(
                    NavigationMenuItem_RenderModel.GetProjection
                );
                var menu = NavigationMenu_RenderModel.GetProjection(menuResponse.Result, menuItems);

                return new ObjectValue(menu);
            }
        );
    }

    /// <summary>
    /// Calls a Raytha Function from a Liquid template.
    /// Usage: raytha_function("developer-name", "methodName", arg1: value1, arg2: value2)
    /// Only functions with trigger type "liquid_template" can be called.
    /// </summary>
    public FunctionValue RaythaFunction()
    {
        return new FunctionValue(
            async (args, context) =>
            {
                // Check if Raytha Functions are enabled
                if (!_raythaFunctionConfiguration.IsEnabled)
                {
                    return NilValue.Instance;
                }

                // Get developer name (arg 0) and method name (arg 1)
                var developerName = args.At(0).ToStringValue();
                var methodName = args.At(1).ToStringValue();

                if (string.IsNullOrEmpty(developerName) || string.IsNullOrEmpty(methodName))
                {
                    return NilValue.Instance;
                }

                // Fetch the Raytha Function from database
                try
                {
                    var response = await _mediator.Send(
                        new GetRaythaFunctionByDeveloperName.Query { DeveloperName = developerName }
                    );

                    var raythaFunction = response.Result;

                    // Validate trigger type is liquid_template
                    if (raythaFunction.TriggerType.DeveloperName != RaythaFunctionTriggerType.LiquidTemplate.DeveloperName)
                    {
                        return NilValue.Instance;
                    }

                    // Check function is active
                    if (!raythaFunction.IsActive)
                    {
                        return NilValue.Instance;
                    }

                    // Collect remaining args (starting from index 2) into a dictionary
                    var functionArgs = new Dictionary<string, object>();
                    var argNames = args.Names.ToList();

                    for (int i = 2; i < args.Count; i++)
                    {
                        var argValue = args.At(i).ToObjectValue();

                        // Check if this arg has a name (named argument)
                        // Named args in Fluid start from index 0 in Names collection
                        var nameIndex = i - 2;
                        if (nameIndex < argNames.Count && !string.IsNullOrEmpty(argNames[nameIndex]))
                        {
                            functionArgs[argNames[nameIndex]] = argValue;
                        }
                        else
                        {
                            // Positional argument
                            functionArgs[$"arg{i - 1}"] = argValue;
                        }
                    }

                    // Serialize args to JSON
                    var argsJson = JsonSerializer.Serialize(functionArgs, _jsonSerializerOptions);

                    // Execute the function
                    var result = await _raythaFunctionScriptEngine.EvaluateInternal(
                        raythaFunction.Code,
                        methodName,
                        argsJson,
                        _raythaFunctionConfiguration.ExecuteTimeout,
                        CancellationToken.None
                    );

                    return FluidValue.Create(result, context.Options);
                }
                catch
                {
                    // Function not found or error during execution
                    return NilValue.Instance;
                }
            }
        );
    }

    private static string ApplyGroupBy(
        FluidValue p,
        string groupByProperty,
        TemplateContext context
    )
    {
        if (groupByProperty.StartsWith("PublishedContent") && groupByProperty.Contains("."))
        {
            var developerName = groupByProperty.Split(".").ElementAt(1);
            return p.GetValueAsync("PublishedContent", context)
                .Result.GetValueAsync(developerName, context)
                .Result.ToStringValue();
        }
        else
        {
            return p.GetValueAsync(groupByProperty, context).Result.ToStringValue();
        }
    }

    private static ValueTask<FluidValue> LocalDateFilter(
        FluidValue input,
        FilterArguments arguments,
        TemplateContext context
    )
    {
        var value = TimeZoneConverter(input, context);
        return ReferenceEquals(value, NilValue.Instance)
            ? value
            : MiscFilters.Date(value, arguments, context);
    }

    private static ValueTask<FluidValue> JsonFilter(
        FluidValue input,
        FilterArguments arguments,
        TemplateContext context
    )
    {
        return new StringValue(
            JsonSerializer.Serialize(input.ToObjectValue(), _jsonSerializerOptions)
        );
    }

    private static FluidValue TimeZoneConverter(FluidValue input, TemplateContext context)
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
