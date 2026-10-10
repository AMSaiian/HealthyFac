using Microsoft.EntityFrameworkCore;
using HealthyFac.Core.Domain.Machines;

namespace HealthyFac.Core.Application;

/// <summary>
/// Declares the sets that use cases query and change. The Infrastructure layer derives the concrete context,
/// adds the provider, the entity configurations, and the interceptors, so the Application layer needs no
/// reference to Infrastructure. Declare every new <see cref="DbSet{TEntity}"/> here.
/// </summary>
/// <param name="options">The options that the derived context passes through.</param>
public abstract class CoreDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Machine> Machines => Set<Machine>();
}
