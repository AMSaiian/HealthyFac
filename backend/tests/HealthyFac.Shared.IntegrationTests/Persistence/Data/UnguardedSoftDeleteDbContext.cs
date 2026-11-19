using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.IntegrationTests.Persistence.Data;

public sealed class UnguardedSoftDeleteDbContext(DbContextOptions<UnguardedSoftDeleteDbContext> options)
    : DbContext(options)
{
    public DbSet<SoftDeleteParentEntity> Parents => Set<SoftDeleteParentEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        SoftDeleteDbContext.ConfigureModel(modelBuilder);
}
