using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Raytha.Application.Common.Behaviors;
using Raytha.Application.Common.Shared;
using Raytha.Application.ContentItems;
using Raytha.Application.ContentItems.Commands;
using Raytha.Application.ContentItems.EventHandlers;
using System.Reflection;
using Raytha.Application.Themes.Commands;
using static Raytha.Application.ContentItems.EventHandlers.ContentItemCreatedEventHandler;

namespace Raytha.Application;

public static class ConfigureServices
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));
        });
        services.AddScoped<BeginExportContentItemsToCsv.BackgroundTask>();
        services.AddScoped<BeginImportContentItemsFromCsv.BackgroundTask>();
        services.AddScoped<BeginImportThemeFromUrl.BackgroundTask>();
        services.AddScoped<BeginMatchWebTemplates.BackgroundTask>();
        services.AddScoped<BeginDuplicateTheme.BackgroundTask>();
        services.AddTransient<RaythaFunctionAsBackgroundTask>();
        services.AddScoped<FieldValueConverter>();
        return services;
    }
}

