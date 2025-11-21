using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using Raytha.Application.Common.Interfaces;

namespace Raytha.Infrastructure.Persistence;

/// <summary>
/// Executes raw database commands that require database-specific SQL.
/// </summary>
public class RaythaRawDbCommands : IRaythaRawDbCommands
{
    private readonly IDbConnection _db;
    private readonly IConfiguration _configuration;

    public RaythaRawDbCommands(IDbConnection db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task ClearAuditLogsAsync(CancellationToken cancellationToken = default)
    {
        var dbProvider = DbProviderHelper.GetDatabaseProviderTypeFromConnectionString(
            _configuration.GetConnectionString("DefaultConnection")
        );

        string query = string.Empty;
        if (dbProvider == DatabaseProviderType.Postgres)
        {
            query = "TRUNCATE TABLE \"AuditLogs\"";
        }
        else
        {
            query = "TRUNCATE TABLE AuditLogs";
        }

        await _db.ExecuteAsync(query);
    }
}

