using System.Data;
using HealthyFac.Shared.Application.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.UnitTests.Conventions.Data;

public sealed class PlantedSyncTransactionStart(DbContext dbContext)
{
    private readonly DbContext _dbContext = dbContext;

    public void Start() => _dbContext.Database.BeginTransaction();
}

public sealed class PlantedAsyncTransactionStart(DbContext dbContext)
{
    private readonly DbContext _dbContext = dbContext;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}

public sealed class PlantedLambdaTransactionStart(DbContext dbContext)
{
    private readonly DbContext _dbContext = dbContext;

    public Func<CancellationToken, Task> Starter =>
        async token =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                token);

            await transaction.CommitAsync(token);
        };
}

public sealed class PlantedTransactionReader(DbContext dbContext)
{
    private readonly DbContext _dbContext = dbContext;

    public bool HasOpenTransaction => _dbContext.Database.CurrentTransaction is not null;
}

public abstract class PlantedTransactionalContextBase(DbContextOptions options)
    : DbContext(options), ITransactionalDbContext
{
    public Task RunInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default(CancellationToken)
    ) => TransactionRunner.RunAsync(
        this,
        async token =>
        {
            await operation(token);

            return true;
        },
        IsolationLevel.ReadCommitted,
        cancellationToken);

    public Task<TResult> RunInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default(CancellationToken)
    ) => TransactionRunner.RunAsync(this, operation, IsolationLevel.ReadCommitted, cancellationToken);

    public Task RunInTransactionAsync(
        Func<CancellationToken, Task> operation,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default(CancellationToken)
    ) => TransactionRunner.RunAsync(
        this,
        async token =>
        {
            await operation(token);

            return true;
        },
        isolationLevel,
        cancellationToken);

    public Task<TResult> RunInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default(CancellationToken)
    ) => TransactionRunner.RunAsync(this, operation, isolationLevel, cancellationToken);
}

public sealed class PlantedContextWithoutSaveOverride(DbContextOptions options)
    : PlantedTransactionalContextBase(options);

public sealed class PlantedContextWithSaveOverride(DbContextOptions options) : PlantedTransactionalContextBase(options)
{
    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default(CancellationToken)
    ) => TransactionRunner.SaveAsync(this, base.SaveChangesAsync, acceptAllChangesOnSuccess, cancellationToken);

    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        TransactionRunner.Save(this, base.SaveChanges, acceptAllChangesOnSuccess);
}

public sealed class PlantedContextWithoutSyncSaveOverride(DbContextOptions options)
    : PlantedTransactionalContextBase(options)
{
    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default(CancellationToken)
    ) => TransactionRunner.SaveAsync(this, base.SaveChangesAsync, acceptAllChangesOnSuccess, cancellationToken);
}
