using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Infrastructure.Messaging;
using HealthyFac.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

public interface ITestServiceDbContext
{
    DbSet<TestAuditedEntity> AuditedEntities { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
}

public sealed class TestServiceDbContext(DbContextOptions<TestServiceDbContext> options)
    : DbContext(options), ITestServiceDbContext, IOutboxDbContext, IInboxDbContext
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    public DbSet<TestAuditedEntity> AuditedEntities => Set<TestAuditedEntity>();

    public Task RunInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default(CancellationToken)
    )
        => RunInTransactionAsync(
            async token =>
            {
                await operation(token);

                return true;
            },
            cancellationToken);

    public async Task<TResult> RunInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        if (ChangeTracker.HasChanges())
        {
            throw PersistenceErrors.UnsavedChangesBeforeTransaction();
        }

        return await Database
            .CreateExecutionStrategy()
            .ExecuteAsync(token => RunTransactionAttemptAsync(operation, token), cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyOutboxConfiguration();
        modelBuilder.ApplyInboxConfiguration();
        modelBuilder.ApplyConfiguration(new TestAuditedEntityConfiguration());
        modelBuilder.ToSnakeCaseConventions();
    }

    private async Task<TResult> RunTransactionAttemptAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken
    )
    {
        ChangeTracker.Clear();
        await using var transaction = await Database.BeginTransactionAsync(cancellationToken);

        var result = await operation(cancellationToken);
        if (Database.CurrentTransaction != transaction)
        {
            throw PersistenceErrors.TransactionEndedByOperation();
        }

        if (ChangeTracker.HasChanges())
        {
            throw PersistenceErrors.UnsavedChangesAfterOperation();
        }

        await transaction.CommitAsync(cancellationToken);

        return result;
    }
}

public sealed record TestBusinessChange : IIntegrationMessage
{
    public static string MessageType => OutboxTestData.BusinessChangeMessageType;

    public required string Text { get; init; }
}

public sealed record TestConcurrentPublish : IIntegrationMessage
{
    public static string MessageType => OutboxTestData.ConcurrentMessageType;

    public required string Text { get; init; }
}

public sealed record TestActorStamped : IIntegrationMessage
{
    public static string MessageType => OutboxTestData.ActorStampedMessageType;

    public required string Text { get; init; }
}

public sealed record TestPing : IIntegrationMessage
{
    public static string MessageType => TransportTestData.PingMessageType;

    public required string Text { get; init; }
}

public sealed class TestPingHandler : IIntegrationMessageHandler<TestPing>
{
    public Task HandleAsync(TestPing message, CancellationToken cancellationToken) => Task.CompletedTask;
}
