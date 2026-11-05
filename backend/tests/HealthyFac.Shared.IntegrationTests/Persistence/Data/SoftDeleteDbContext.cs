using HealthyFac.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.IntegrationTests.Persistence.Data;

public sealed class SoftDeleteDbContext(DbContextOptions<SoftDeleteDbContext> options) : DbContext(options)
{
    public DbSet<SoftDeleteTestEntity> SoftDeletables => Set<SoftDeleteTestEntity>();

    public DbSet<PlainTestEntity> Plains => Set<PlainTestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SoftDeleteTestEntity>().ConfigureEntity().ConfigureSoftDeletable();
        modelBuilder.Entity<PlainTestEntity>().ConfigureEntity();
    }
}
