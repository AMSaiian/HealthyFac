using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Infrastructure.Messaging;
using HealthyFac.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.UnitTests.Messaging.Data;

public interface ITestServiceDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
}

public sealed class TestServiceDbContext(DbContextOptions<TestServiceDbContext> options)
    : DbContext(options), ITestServiceDbContext, IOutboxDbContext, IInboxDbContext
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    public Task RunInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default(CancellationToken)
    )
        => throw new NotSupportedException();

    public Task<TResult> RunInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default(CancellationToken)
    )
        => throw new NotSupportedException();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyOutboxConfiguration();
        modelBuilder.ApplyInboxConfiguration();
        modelBuilder.ToSnakeCaseConventions();
    }
}

public sealed record TestPing : IIntegrationMessage
{
    public static string MessageType => MessagingTestData.PingMessageType;

    public required string Text { get; init; }
}
