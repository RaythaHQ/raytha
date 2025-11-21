using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using Raytha.Infrastructure.BackgroundTasks;
using Raytha.Infrastructure.Configurations;
using Raytha.Infrastructure.FileStorage;
using Raytha.Infrastructure.JsonQueryEngine.Postgres;
using Raytha.Infrastructure.JsonQueryEngine.SqlServer;
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

        var dbProviderType = DbProviderHelper.GetDatabaseProviderTypeFromConnectionString(
            dbConnectionString
        );

        services.AddScoped<AuditableEntitySaveChangesInterceptor>();

        if (dbProviderType == DatabaseProviderType.Postgres)
        {
            services.AddDbContext<RaythaDbContext>(options =>
            {
                options.UseNpgsql(
                    dbConnectionString,
                    npgsqlOptions =>
                    {
                        npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5);
                        npgsqlOptions.MigrationsAssembly("Raytha.Migrations.Postgres");
                    }
                );
            });
            services.AddTransient<IRaythaDbJsonQueryEngine, RaythaDbPostgresJsonQueryEngine>();
            services.AddTransient<IDbConnection>(_ => new NpgsqlConnection(dbConnectionString));
            services.AddScoped<IRaythaDbContext>(provider =>
                provider.GetRequiredService<RaythaDbContext>()
            );

            // Health checks for PostgreSQL
            services
                .AddHealthChecks()
                .AddNpgSql(dbConnectionString, name: "postgres", timeout: TimeSpan.FromSeconds(5));
        }
        else
        {
            services.AddDbContext<RaythaDbContext>(options =>
            {
                options.UseSqlServer(
                    dbConnectionString,
                    sqlServerOptions =>
                    {
                        sqlServerOptions.EnableRetryOnFailure(maxRetryCount: 5);
                        sqlServerOptions.MigrationsAssembly("Raytha.Migrations.SqlServer");
                    }
                );
            });
            services.AddTransient<IRaythaDbJsonQueryEngine, RaythaDbSqlServerJsonQueryEngine>();
            services.AddTransient<IDbConnection>(_ => new SqlConnection(dbConnectionString));
            services.AddScoped<IRaythaDbContext>(provider =>
                provider.GetRequiredService<RaythaDbContext>()
            );

            // Health checks for SQL Server
            services
                .AddHealthChecks()
                .AddSqlServer(
                    dbConnectionString,
                    name: "sqlserver",
                    timeout: TimeSpan.FromSeconds(5)
                );
        }

        services.AddSingleton<
            ICurrentOrganizationConfiguration,
            CurrentOrganizationConfiguration
        >();
        services.AddSingleton<IRaythaFunctionConfiguration, RaythaFunctionConfiguration>();
        services.AddScoped<IEmailerConfiguration, EmailerConfiguration>();

        services.AddScoped<IEmailer, Emailer>();
        services.AddTransient<IBackgroundTaskDb, BackgroundTaskDb>();
        services.AddTransient<IRaythaRawDbInfo, RaythaRawDbInfo>();
        services.AddTransient<IRaythaRawDbCommands, RaythaRawDbCommands>();

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
        services.AddTransient<IRaythaFunctionScriptEngine, RaythaFunctionScriptEngine>();
        services.AddScoped<IRaythaFunctionApi_V1, RaythaFunctionApi_V1>();
        services.AddSingleton<IRaythaFunctionSemaphore, RaythaFunctionSemaphore>();

        return services;
    }
}
