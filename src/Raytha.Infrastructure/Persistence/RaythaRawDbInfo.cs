using System.Data;
using Dapper;
using Raytha.Application.Common.Interfaces;

namespace Raytha.Infrastructure.Persistence;

/// <summary>
/// Retrieves raw database information.
/// </summary>
public class RaythaRawDbInfo : IRaythaRawDbInfo
{
    private readonly IDbConnection _db;

    public RaythaRawDbInfo(IDbConnection db)
    {
        _db = db;
    }

    public DbSpaceUsed GetDatabaseSize()
    {
        const string query =
            "SELECT pg_size_pretty(pg_database_size(current_database())) AS reserved FROM pg_class LIMIT 1;";
        return _db.QueryFirst<DbSpaceUsed>(query);
    }
}
