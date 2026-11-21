using System.Data.Common;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

public sealed record TestRealtimePush : IIntegrationMessage, IRequest
{
    public static string MessageType => InboxRealtimeTestData.MessageType;

    public required string Text { get; init; }
}

/// <summary>
/// Adds one group event for each handling. A text that holds <see cref="InboxRealtimeTestData.FailsOnceMarker"/>
/// makes the first handling fail after the event was added, as a handler does that fails after its work.
/// </summary>
public sealed class RealtimePushHandler(
    IRealtimeNotifier notifier,
    ITestServiceDbContext dbContext,
    ICurrentUserProvider currentUserProvider,
    HandlerRecorder recorder
) : RequestHandler<TestRealtimePush>
{
    private readonly IRealtimeNotifier _notifier = notifier;
    private readonly ITestServiceDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUserProvider = currentUserProvider;
    private readonly HandlerRecorder _recorder = recorder;

    protected override async Task HandleInternalAsync(TestRealtimePush request, CancellationToken cancellationToken)
    {
        var attempts = _recorder.CallsFor(request.Text).Count;
        _recorder.Record(new HandledCall(request.Text, _currentUserProvider.TryGetActor()));
        _dbContext.AuditedEntities.Add(new TestAuditedEntity(request.Text));
        await _dbContext.SaveChangesAsync(cancellationToken);
        _notifier.AddToGroup(
            InboxProcessorTestData.UserCompanyId,
            RealtimeNotificationKind.WorkOrderBlocked,
            new { request.Text },
            isActorSkipped: true);

        var isFailing = request.Text.Contains(InboxRealtimeTestData.FailsOnceMarker) && attempts == 0;
        if (isFailing)
        {
            throw new InvalidOperationException(InboxProcessorTestData.HandlerFailureText);
        }
    }
}

public sealed class TestRealtimePushPreHandlerCheck : PassThroughPreHandlerCheck<TestRealtimePush>;

/// <summary>
/// Fails the first command that marks an inbox row as processed with a transient Npgsql error, so the execution
/// strategy repeats the whole operation of the inbox runner.
/// </summary>
public sealed class InboxUpdateFailureInterceptor : DbCommandInterceptor
{
    private int _injectedCount;

    public int InjectedCount => Volatile.Read(ref _injectedCount);

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        FailFirstInboxUpdate(command);

        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        FailFirstInboxUpdate(command);

        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    private void FailFirstInboxUpdate(DbCommand command)
    {
        var isInboxUpdate = command.CommandText.Contains(InboxRealtimeTestData.UpdateCommand)
            && command.CommandText.Contains(DatabaseOutageTestData.InboxTableName);
        if (isInboxUpdate && Interlocked.Increment(ref _injectedCount) == 1)
        {
            throw new NpgsqlException(DatabaseOutageTestData.InjectedFailureText, new TimeoutException());
        }
    }
}

/// <summary>
/// The transport fixture with a retry strategy and an interceptor that fails the first inbox update.
/// </summary>
public sealed class InboxRealtimeRetryFixture : TransportFixture
{
    private readonly InboxUpdateFailureInterceptor _interceptor = new();

    public int InjectedFailureCount => _interceptor.InjectedCount;

    protected override void ConfigureDbContext(DbContextOptionsBuilder options, string connectionString)
    {
        options.UseNpgsql(
                connectionString,
                npgsql => npgsql.EnableRetryOnFailure(
                    DatabaseOutageTestData.RetryMaxCount,
                    DatabaseOutageTestData.RetryMaxDelay,
                    errorCodesToAdd: null))
            .AddInterceptors(_interceptor);
    }
}
