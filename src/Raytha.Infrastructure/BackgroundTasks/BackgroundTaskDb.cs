using Dapper;
using Microsoft.Extensions.Configuration;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Entities;
using Raytha.Infrastructure.Persistence;
using System.Data;

namespace Raytha.Infrastructure.BackgroundTasks;

public class BackgroundTaskDb : IBackgroundTaskDb
{
    private readonly IDbConnection _db;
    private readonly IConfiguration _configuration;
    public BackgroundTaskDb(IDbConnection db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public BackgroundTask DequeueBackgroundTask()
    {
        var dbProvider = DbProviderHelper.GetDatabaseProviderTypeFromConnectionString(_configuration.GetConnectionString("DefaultConnection"));
        BackgroundTask bgTask;
        if (_db.State == ConnectionState.Closed)
        {
            _db.Open();
        }
        using (var transaction = _db.BeginTransaction())
        {
            if (dbProvider == DatabaseProviderType.Postgres)
            {
                bgTask = _db.QueryFirstOrDefault<BackgroundTask>("SELECT * FROM \"BackgroundTasks\" WHERE \"Status\" = @status ORDER BY \"CreationTime\" ASC FOR UPDATE SKIP LOCKED", new { status = BackgroundTaskStatus.Enqueued.DeveloperName }, transaction: transaction); 
                if (bgTask != null)
                {
                    _db.Execute("UPDATE \"BackgroundTasks\" SET \"Status\" = @status WHERE \"Id\" = @id", new { status = BackgroundTaskStatus.Processing.DeveloperName, id = bgTask.Id }, transaction: transaction);
                }
            }
            else
            {
                bgTask = _db.QueryFirstOrDefault<BackgroundTask>("SELECT * FROM BackgroundTasks WITH (readpast, updlock) WHERE Status = @status ORDER BY CreationTime ASC", new { status = BackgroundTaskStatus.Enqueued.DeveloperName }, transaction: transaction);
                if (bgTask != null)
                {
                    _db.Execute("UPDATE BackgroundTasks SET Status = @status WHERE Id = @id", new { status = BackgroundTaskStatus.Processing.DeveloperName, id = bgTask.Id }, transaction: transaction);
                }
            }
            transaction.Commit();
        }
        return bgTask;
    }
}
