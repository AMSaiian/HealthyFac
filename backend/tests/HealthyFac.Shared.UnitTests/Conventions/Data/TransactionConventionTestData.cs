using System.Data;
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
