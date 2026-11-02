using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.IntegrationTests.Concurrency.Data;

public sealed class GateDbContext(DbContextOptions<GateDbContext> options) : DbContext(options)
{
    public DbSet<GateRecord> Records => Set<GateRecord>();
}

public sealed class GateRecord
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
}
