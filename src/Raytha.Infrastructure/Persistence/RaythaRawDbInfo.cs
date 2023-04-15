using Dapper;
using Raytha.Application.Common.Interfaces;
using System.Data;

namespace Raytha.Infrastructure.Persistence;

public class RaythaRawDbInfo : IRaythaRawDbInfo
{
    private readonly IDbConnection _db;
    public RaythaRawDbInfo(IDbConnection db)
    {
        _db = db;
    }

    public DbSpaceUsed GetDatabaseSize()
    {
        DbSpaceUsed dbSizeInfo = _db.QueryFirst<DbSpaceUsed>("EXEC sp_spaceused @oneresultset = 1");
        return dbSizeInfo;
    }
}
