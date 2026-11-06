using HealthyFac.Auth.Application;
using HealthyFac.Shared.Infrastructure.Messaging;
using HealthyFac.Shared.Infrastructure.Messaging.Receiving;
using HealthyFac.Shared.Infrastructure.Messaging.Sending;
using HealthyFac.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Auth.Infrastructure.Database;

/// <summary>
/// Database context for the auth service. Use cases reach it through <see cref="IAuthDbContext"/>, so they need
/// no reference to this layer. The outbox and inbox reach it through their own interfaces, and
/// <see cref="MessagingExtensions"/> maps every interface to one scoped instance.
/// </summary>
/// <param name="options">The options for this context.</param>
public class AuthDbContext(DbContextOptions<AuthDbContext> options)
    : DbContext(options), IAuthDbContext, IOutboxDbContext, IInboxDbContext
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

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
        modelBuilder.UseCaseInsensitiveText();
        modelBuilder.ToSnakeCaseConventions();
        modelBuilder.HasPostgreSqlExtensions(DatabaseConstants.RequiredPostgresExtensions);
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
