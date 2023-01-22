using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using Raytha.Infrastructure.FileStorage;
using Raytha.Infrastructure.JsonQueryEngine;
using Raytha.Infrastructure.Persistence;
using Raytha.Infrastructure.Persistence.Interceptors;
using Raytha.Infrastructure.Services;
using System.Data;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        switch (configuration["DATABASE_PROVIDER"])
        {
            case "mssql":
                var mssqlConnectionString = configuration.GetConnectionString("mssqlConnection");
                services.AddDbContext<RaythaDbContext>(options =>
                    options.UseSqlServer(
                        mssqlConnectionString,
                        b => b.MigrationsAssembly("Raytha.Infrastructure.Migrations.Mssql")));
                services.AddTransient<IDbConnection>((sp) => new SqlConnection(mssqlConnectionString));
                break;
            case "mysql":
                var mysqlConnectionString = configuration.GetConnectionString("mysqlConnection");
                var serverVersion = ServerVersion.AutoDetect(mysqlConnectionString);
                services.AddDbContext<RaythaDbContext>(options =>
                    options.UseMySql(
                        mysqlConnectionString,
                        serverVersion,
                        b => b.MigrationsAssembly("Raytha.Infrastructure.Migrations.Mysql")));
                services.AddTransient<IDbConnection>((sp) => new MySqlConnection(mysqlConnectionString));
                break;
            default:
                throw new Exception("database provider unknown");
        }

        //entity framework
        services.AddScoped<AuditableEntitySaveChangesInterceptor>();

        services.AddScoped<IRaythaDbContext>(provider => provider.GetRequiredService<RaythaDbContext>());

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