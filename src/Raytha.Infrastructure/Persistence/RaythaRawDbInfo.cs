using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using Raytha.Application.Common.Interfaces;

namespace Raytha.Infrastructure.Persistence;

public class RaythaRawDbInfo : IRaythaRawDbInfo
{
    private readonly IDbConnection _db;
    private readonly IConfiguration _configuration;

    public RaythaRawDbInfo(IDbConnection db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public DbSpaceUsed GetDatabaseSize()
    {
        var dbProvider = DbProviderHelper.GetDatabaseProviderTypeFromConnectionString(
            _configuration.GetConnectionString("DefaultConnection")
        );
        string query = string.Empty;
        if (dbProvider == DatabaseProviderType.Postgres)
        {
            query =
                "SELECT pg_size_pretty(pg_database_size(current_database())) AS reserved FROM pg_class LIMIT 1;";
        }
        else
        {
            query = "EXEC sp_spaceused @oneresultset = 1";
        }
        DbSpaceUsed dbSizeInfo = _db.QueryFirst<DbSpaceUsed>(query);
        return dbSizeInfo;
    }
}
