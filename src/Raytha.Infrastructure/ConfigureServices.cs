using Raytha.Infrastructure.Persistence.Interceptors;
using Raytha.Application.Common.Interfaces;
using Raytha.Infrastructure.Persistence;
using Raytha.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.Data;
using Raytha.Infrastructure.JsonQueryEngine;
using Raytha.Infrastructure.FileStorage;
using Raytha.Application.Common.Utils;
using Raytha.Infrastructure.BackgroundTasks;
using Microsoft.Extensions.Hosting;
using Raytha.Application.ContentItems;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var dbConnectionString = configuration.GetConnectionString("DefaultConnection");

        //entity framework
        services.AddScoped<AuditableEntitySaveChangesInterceptor>();
        services.AddDbContext<RaythaDbContext>(options =>
            options.UseSqlServer(dbConnectionString));

        services.AddScoped<IRaythaDbContext>(provider => provider.GetRequiredService<RaythaDbContext>());

        //direct to db
        services.AddTransient<IDbConnection>((sp) => new SqlConnection(dbConnectionString));

        services.AddScoped<IEmailer, Emailer>();
        services.AddTransient<IRaythaDbJsonQueryEngine, RaythaDbJsonQueryEngine>();
        services.AddTransient<IBackgroundTaskDb, BackgroundTaskDb>();
        services.AddTransient<IRaythaRawDbInfo, RaythaRawDbInfo>();

        //file storage provider
        var fileStorageProvider = configuration[FileStorageUtility.CONFIG_NAME].IfNullOrEmpty(FileStorageUtility.LOCAL).ToLower();
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
            throw new NotImplementedException($"Unsupported file storage provider: {fileStorageProvider}");
        }

        for (int i = 0; i < Convert.ToInt32(configuration["NUM_BACKGROUND_WORKERS"] ?? "4"); i++)
        {
            services.AddSingleton<IHostedService, QueuedHostedService>();
        }
        services.AddScoped<IBackgroundTaskQueue, BackgroundTaskQueue>();

        return services;
    }
}