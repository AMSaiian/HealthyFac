using Microsoft.EntityFrameworkCore;
using HealthyFac.Core.Application;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Shared.Infrastructure.Persistence;

namespace HealthyFac.Core.Infrastructure.Database;

/// <summary>
/// Database context for the core service. The sets live on <see cref="CoreDbContext"/>, so use cases reach
/// them without a reference to this layer.
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : CoreDbContext(options)
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.HaveEnumerationConversion<MachineState>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        modelBuilder.ToSnakeCaseConventions();
        modelBuilder.HasPostgreSqlExtensions(DatabaseConstants.RequiredPostgresExtensions);
    }
}
