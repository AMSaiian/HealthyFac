using HealthyFac.Testing.Containers;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.IntegrationTests.Grids.Data;

public sealed class GridDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlTestDatabase _database = new();

    public IReadOnlyList<GridTestEntity> Rows { get; private set; } = [];

    public async ValueTask InitializeAsync()
    {
        await _database.StartAsync();
        var rows = GridTestData.CreateRows();
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync();
        dbContext.Rows.AddRange(rows);
        await dbContext.SaveChangesAsync();
        Rows = rows;
    }

    public async ValueTask DisposeAsync() => await _database.DisposeAsync();

    public GridTestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GridTestDbContext>().UseNpgsql(_database.ConnectionString).Options;

        return new GridTestDbContext(options);
    }
}
