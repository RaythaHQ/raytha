using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Raytha.Application.Common.Behaviors;
using Raytha.Application.ContentItems;
using Raytha.Application.ContentItems.Commands;
using System.Reflection;

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
        services.AddScoped<FieldValueConverter>();
        return services;
    }
}

