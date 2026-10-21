using Microsoft.EntityFrameworkCore;
using HealthyFac.Core.Domain.Machines;

namespace HealthyFac.Core.Application;

/// <summary>
/// Declares the sets and the <see cref="DbContext"/> members that use cases need. The Infrastructure layer
/// implements it, so the Application layer needs no reference to Infrastructure. Declare every new
/// <see cref="DbSet{TEntity}"/> here. Copy a <see cref="DbContext"/> member signature only when a use case calls it.
/// </summary>
public interface ICoreDbContext
{
    DbSet<Machine> Machines { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
}
