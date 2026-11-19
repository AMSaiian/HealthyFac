using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.Infrastructure.Persistence.SoftDelete.Dependents;

/// <summary>
/// Soft-deletes the untracked rows of one dependent type under deleted principals. The interceptor reads the
/// dependent type from the model at run time, so a generic implementation
/// (<see cref="DependentSoftDelete{TDependent}"/>) gives the type to <c>ExecuteUpdate</c>.
/// </summary>
internal interface IDependentSoftDelete
{
    /// <summary>
    /// Soft-deletes the rows whose foreign key of the relation is in the principal keys, except the rows that the
    /// context tracks. A row that is already soft-deleted keeps its delete time.
    /// </summary>
    /// <param name="dbContext">The context that is about to save. It needs an open transaction.</param>
    /// <param name="deletion">The relation, the keys, the delete time, and the actor.</param>
    /// <param name="cancellationToken">Cancels the statements.</param>
    /// <returns>
    /// The keys of the changed rows when <see cref="DependentDeletionContext.HasOwnCascade"/> is true, else an empty
    /// list.
    /// </returns>
    Task<List<Guid>> ApplyAsync(
        DbContext dbContext,
        DependentDeletionContext deletion,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Does the work of <see cref="ApplyAsync"/> with the synchronous EF Core calls. It never blocks on a task.
    /// </summary>
    /// <param name="dbContext">The context that is about to save. It needs an open transaction.</param>
    /// <param name="deletion">The relation, the keys, the delete time, and the actor.</param>
    /// <returns>
    /// The keys of the changed rows when <see cref="DependentDeletionContext.HasOwnCascade"/> is true, else an empty
    /// list.
    /// </returns>
    List<Guid> Apply(DbContext dbContext, DependentDeletionContext deletion);
}
