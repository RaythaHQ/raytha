using CSharpVitamins;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Raytha.Application.Common.Behaviors;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Users;
using Raytha.Application.Users.Commands;
using Raytha.Application.Users.Queries;

namespace Raytha.Application.UnitTests.Common.Behaviors;

public class TransactionBehaviorTests
{
    private Mock<DatabaseFacade> _database = null!;
    private Mock<IDbContextTransaction> _transaction = null!;
    private Mock<IRaythaDbContext> _db = null!;

    [SetUp]
    public void Setup()
    {
        var context = new Mock<DbContext>();
        _database = new Mock<DatabaseFacade>(MockBehavior.Strict, context.Object);
        _transaction = new Mock<IDbContextTransaction>(MockBehavior.Strict);
        context.Setup(c => c.Database).Returns(_database.Object);
        _db = new Mock<IRaythaDbContext>(MockBehavior.Strict);
        _db.Setup(d => d.DbContext).Returns(context.Object);
    }

    [Test]
    public async Task A_query_is_not_wrapped_in_a_transaction()
    {
        var behavior = new TransactionBehavior<GetUserById.Query, IQueryResponseDto<UserDto>>(
            _db.Object
        );
        var expected = new QueryResponseDto<UserDto>(new UserDto { EmailAddress = "a@b.com" });

        var response = await behavior.Handle(
            new GetUserById.Query { Id = ShortGuid.NewGuid() },
            (_, _) => ValueTask.FromResult<IQueryResponseDto<UserDto>>(expected),
            CancellationToken.None
        );

        response.Result.EmailAddress.Should().Be("a@b.com");
        _db.Verify(d => d.DbContext, Times.Never);
    }

    [Test]
    public async Task A_command_sent_inside_an_open_transaction_joins_it()
    {
        _database.Setup(d => d.CurrentTransaction).Returns(_transaction.Object);
        var behavior = new TransactionBehavior<CreateUser.Command, CommandResponseDto<ShortGuid>>(
            _db.Object
        );
        var id = ShortGuid.NewGuid();

        var response = await behavior.Handle(
            new CreateUser.Command { EmailAddress = "a@b.com" },
            (_, _) => ValueTask.FromResult(new CommandResponseDto<ShortGuid>(id)),
            CancellationToken.None
        );

        response.Result.Should().Be(id);
        _database.Verify(d => d.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task A_command_commits_after_its_handler_succeeds()
    {
        ArrangeNoOpenTransaction();
        var steps = new List<string>();
        _database
            .Setup(d => d.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Callback(() => steps.Add("begin"))
            .ReturnsAsync(_transaction.Object);
        _transaction
            .Setup(t => t.CommitAsync(It.IsAny<CancellationToken>()))
            .Callback(() => steps.Add("commit"))
            .Returns(Task.CompletedTask);
        _transaction
            .Setup(t => t.DisposeAsync())
            .Callback(() => steps.Add("dispose"))
            .Returns(ValueTask.CompletedTask);
        var behavior = new TransactionBehavior<CreateUser.Command, CommandResponseDto<ShortGuid>>(
            _db.Object
        );
        var id = ShortGuid.NewGuid();

        var response = await behavior.Handle(
            new CreateUser.Command { EmailAddress = "a@b.com" },
            (_, _) =>
            {
                steps.Add("handler");
                return ValueTask.FromResult(new CommandResponseDto<ShortGuid>(id));
            },
            CancellationToken.None
        );

        response.Result.Should().Be(id);
        steps.Should().Equal("begin", "handler", "commit", "dispose");
    }

    [Test]
    public async Task A_command_whose_handler_throws_is_not_committed()
    {
        ArrangeNoOpenTransaction();
        var behavior = new TransactionBehavior<CreateUser.Command, CommandResponseDto<ShortGuid>>(
            _db.Object
        );

        var act = async () =>
            await behavior.Handle(
                new CreateUser.Command { EmailAddress = "a@b.com" },
                (_, _) => throw new InvalidOperationException("handler failed"),
                CancellationToken.None
            );

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("handler failed");
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _transaction.Verify(t => t.DisposeAsync(), Times.Once);
    }

    [Test]
    public async Task A_command_that_reports_failure_still_commits()
    {
        ArrangeNoOpenTransaction();
        _transaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var behavior = new TransactionBehavior<CreateUser.Command, CommandResponseDto<ShortGuid>>(
            _db.Object
        );

        var response = await behavior.Handle(
            new CreateUser.Command(),
            (_, _) =>
                ValueTask.FromResult(new CommandResponseDto<ShortGuid>("EmailAddress", "taken")),
            CancellationToken.None
        );

        response.Success.Should().BeFalse();
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private void ArrangeNoOpenTransaction()
    {
        _database.Setup(d => d.CurrentTransaction).Returns((IDbContextTransaction?)null);
        _database.Setup(d => d.CreateExecutionStrategy()).Returns(new RunOnceExecutionStrategy());
        _database
            .Setup(d => d.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transaction.Object);
        _transaction.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);
    }

    /// <summary>
    /// Stands in for NpgsqlRetryingExecutionStrategy so these tests observe begin, commit, and
    /// dispose without a database. The retry path itself is proven against Postgres.
    /// </summary>
    private sealed class RunOnceExecutionStrategy : IExecutionStrategy
    {
        public bool RetriesOnFailure => false;

        public TResult Execute<TState, TResult>(
            TState state,
            Func<DbContext, TState, TResult> operation,
            Func<DbContext, TState, ExecutionResult<TResult>>? verifySucceeded
        ) => operation(null!, state);

        public Task<TResult> ExecuteAsync<TState, TResult>(
            TState state,
            Func<DbContext, TState, CancellationToken, Task<TResult>> operation,
            Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded,
            CancellationToken cancellationToken = default
        ) => operation(null!, state, cancellationToken);
    }
}
