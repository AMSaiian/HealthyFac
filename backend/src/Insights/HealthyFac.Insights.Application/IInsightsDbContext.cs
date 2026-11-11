using HealthyFac.Insights.Domain.Documents;
using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Shared.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace HealthyFac.Insights.Application;

/// <summary>
/// Declares the sets and the <see cref="DbContext"/> members that use cases need. The Infrastructure layer
/// implements it, so the Application layer needs no reference to Infrastructure. Declare every new
/// <see cref="DbSet{TEntity}"/> here. Copy a <see cref="DbContext"/> member signature only when a use case calls it.
/// </summary>
public interface IInsightsDbContext : ITransactionalDbContext
{
    DbSet<IngestAttempt> IngestAttempts { get; }

    DbSet<IngestedDocument> IngestedDocuments { get; }

    DbSet<DocumentChunk> DocumentChunks { get; }

    EntityEntry<TEntity> Entry<TEntity>(TEntity entity)
        where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
}
