using HealthyFac.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.IntegrationTests.Persistence.Data;

/// <summary>
/// A context without entities. It runs raw SQL through the same Npgsql options that a service context uses.
/// </summary>
public sealed class TracingDbContext(string connectionString) : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseNpgsql(connectionString, npgsql => npgsql.ConfigureParentOnlyTracing());
}
