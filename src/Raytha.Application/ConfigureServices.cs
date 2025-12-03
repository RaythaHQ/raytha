using System.Reflection;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Raytha.Application.Common.Behaviors;
using Raytha.Application.Common.Shared;
using Raytha.Application.ContentItems;
using Raytha.Application.ContentItems.Commands;
using Raytha.Application.ContentItems.EventHandlers;
using Raytha.Application.Themes.Commands;
using static Raytha.Application.ContentItems.EventHandlers.ContentItemCreatedEventHandler;

namespace Raytha.Application;

public static class ConfigureServices
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddMediator(options =>
        {
            options.Namespace = "Raytha.Application.Mediator";
            options.ServiceLifetime = ServiceLifetime.Scoped;
        });
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));
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
