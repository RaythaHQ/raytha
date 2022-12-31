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

        return services;
    }
}