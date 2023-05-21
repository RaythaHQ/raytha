using Dapper;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Entities;
using System.Data;

namespace Raytha.Infrastructure.BackgroundTasks;

public class BackgroundTaskDb : IBackgroundTaskDb
{
    private readonly IDbConnection _db;
    public BackgroundTaskDb(IDbConnection db)
    {
        _db = db;
    }

    public BackgroundTask DequeueBackgroundTask()
    {
        BackgroundTask bgTask;
        if (_db.State == ConnectionState.Closed)
        {
            _db.Open();
        }
        using (var transaction = _db.BeginTransaction())
        {
            bgTask = _db.QueryFirstOrDefault<BackgroundTask>("SELECT * FROM BackgroundTasks WITH (readpast, updlock) WHERE Status = @status ORDER BY CreationTime ASC", new { status = BackgroundTaskStatus.Enqueued.DeveloperName }, transaction: transaction);
            if (bgTask != null)
            {
                _db.Execute("UPDATE BackgroundTasks SET Status = @status WHERE Id = @id", new { status = BackgroundTaskStatus.Processing.DeveloperName, id = bgTask.Id }, transaction: transaction);
            }
            transaction.Commit();
        }
        return bgTask;
    }
}
