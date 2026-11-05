using HealthyFac.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.IntegrationTests.Grids.Data;

public sealed class GridTestDbContext(DbContextOptions<GridTestDbContext> options) : DbContext(options)
{
    public DbSet<GridTestEntity> Rows => Set<GridTestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GridTestEntity>().ConfigureEntity();
        modelBuilder.UseCaseInsensitiveText();
    }
}
