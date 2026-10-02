using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Common.Behaviors;

/// <summary>
/// Runs a command's handler, its after-save domain event handlers, its audit row, and its
/// webhook publish in one database transaction. Queries pass straight through, and a command
/// sent from inside another command joins the transaction already open.
/// </summary>
public sealed class TransactionBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private static readonly bool IsCommand =
        typeof(ILoggableRequest).IsAssignableFrom(typeof(TMessage))
        || typeof(ILoggableEntityRequest).IsAssignableFrom(typeof(TMessage));

    private readonly IRaythaDbContext _db;

    public TransactionBehavior(IRaythaDbContext db)
    {
        _db = db;
    }

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken
    )
    {
        if (!IsCommand)
        {
            return await next(message, cancellationToken);
        }

        var database = _db.DbContext.Database;
        if (database.CurrentTransaction is not null)
        {
            return await next(message, cancellationToken);
        }

        // Npgsql is configured with EnableRetryOnFailure, and that strategy refuses a
        // user-initiated transaction unless the whole unit of work runs inside ExecuteAsync.
        var firstAttempt = true;
        return await database
            .CreateExecutionStrategy()
            .ExecuteAsync(
                async token =>
                {
                    if (!firstAttempt)
                    {
                        // A retry re-runs the handler, so entities the failed attempt left
                        // tracked as Added would otherwise be inserted a second time.
                        _db.DbContext.ChangeTracker.Clear();
                    }
                    firstAttempt = false;

                    await using var transaction = await database.BeginTransactionAsync(token);
                    var response = await next(message, token);
                    await transaction.CommitAsync(token);
                    return response;
                },
                cancellationToken
            );
    }
}
