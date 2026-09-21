using System;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using Raytha.Application.Maintenance;
using Raytha.Infrastructure.BackgroundTasks;
using Raytha.Infrastructure.Configurations;
using Raytha.Infrastructure.FileStorage;
using Raytha.Infrastructure.Health;
using Raytha.Infrastructure.JsonQueryEngine.Postgres;
using Raytha.Infrastructure.Maintenance;
using Raytha.Infrastructure.Persistence;
using Raytha.Infrastructure.Persistence.Interceptors;
using Raytha.Infrastructure.RaythaFunctions;
using Raytha.Infrastructure.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var dbConnectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddScoped<AuditableEntitySaveChangesInterceptor>();

        services.AddDbContext<RaythaDbContext>(options =>
        {
            options.UseNpgsql(
                dbConnectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5);
                }
            );
        });
        services.AddTransient<IRaythaDbJsonQueryEngine, RaythaDbPostgresJsonQueryEngine>();
        services.AddTransient<IDbConnection>(_ => new NpgsqlConnection(dbConnectionString));
        services.AddScoped<IRaythaDbContext>(provider =>
            provider.GetRequiredService<RaythaDbContext>()
        );

        // Readiness checks are tagged "ready"; /healthz (liveness) runs no checks at all.
        services
            .AddHealthChecks()
            .AddNpgSql(
                dbConnectionString,
                name: "postgres",
                timeout: TimeSpan.FromSeconds(5),
                tags: new[] { HealthCheckTags.Ready }
            )
            .AddCheck<FileStorageHealthCheck>(
                FileStorageHealthCheck.Name,
                tags: new[] { HealthCheckTags.Ready }
            );

        services.AddSingleton<
            ICurrentOrganizationConfiguration,
            CurrentOrganizationConfiguration
        >();
        services.AddSingleton<IRaythaFunctionConfiguration, RaythaFunctionConfiguration>();
        services.AddSingleton<ISecurityConfiguration, SecurityConfiguration>();
        services.AddScoped<IEmailerConfiguration, EmailerConfiguration>();

        // IEmailer is decorated so every send attempt lands in the EmailLogs table.
        services.AddScoped<Emailer>();
        services.AddScoped<IEmailer>(provider => new EmailLoggingEmailer(
            provider.GetRequiredService<Emailer>(),
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<ILogger<EmailLoggingEmailer>>()
        ));
        services.AddTransient<IBackgroundTaskDb, BackgroundTaskDb>();
        services.AddTransient<IRaythaRawDbInfo, RaythaRawDbInfo>();
        services.AddTransient<IRaythaRawDbCommands, RaythaRawDbCommands>();
        services.AddScoped<IMaintenanceQueryService, MaintenanceQueryService>();

        //file storage provider
        var fileStorageProvider = configuration[FileStorageUtility.CONFIG_NAME]
            .IfNullOrEmpty(FileStorageUtility.LOCAL)
            .ToLower();
        if (fileStorageProvider == FileStorageUtility.LOCAL)
        {
            services.AddScoped<IFileStorageProvider, LocalFileStorageProvider>();
        }
        else if (fileStorageProvider.ToLower() == FileStorageUtility.AZUREBLOB)
        {
            services.AddScoped<IFileStorageProvider, AzureBlobFileStorageProvider>();
        }
        else if (fileStorageProvider.ToLower() == FileStorageUtility.S3)
        {
            services.AddScoped<IFileStorageProvider, S3FileStorageProvider>();
        }
        else
        {
            throw new NotImplementedException(
                $"Unsupported file storage provider: {fileStorageProvider}"
            );
        }

        for (int i = 0; i < Convert.ToInt32(configuration["NUM_BACKGROUND_WORKERS"] ?? "4"); i++)
        {
            services.AddSingleton<IHostedService, QueuedHostedService>();
        }
        services.AddScoped<IBackgroundTaskQueue, BackgroundTaskQueue>();

        services.AddHttpClient<IRaythaFunctionsHttpClient, RaythaFunctionsHttpClient>();
        services.AddSingleton<IV8EnginePool, V8EnginePool>();
        services.AddScoped<IRaythaFunctionScriptEngine, RaythaFunctionScriptEngine>();
        services.AddScoped<IRaythaFunctionApi_V1, RaythaFunctionApi_V1>();
        services.AddSingleton<IRaythaFunctionSemaphore, RaythaFunctionSemaphore>();

        return services;
    }
}
