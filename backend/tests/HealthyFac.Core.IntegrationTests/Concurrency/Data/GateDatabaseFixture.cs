using HealthyFac.Testing.Containers;
using HealthyFac.Testing.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.IntegrationTests.Concurrency.Data;

public sealed class GateDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlTestDatabase _database = new();

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => _database.DisposeAsync().AsTask();

    public async Task SaveRecordAsync(ConcurrentSaveGate gate)
    {
        await using var context = CreateContext(gate);
        context.Records.Add(new GateRecord());

        await context.SaveChangesAsync();
    }

    private GateDbContext CreateContext(ConcurrentSaveGate? gate = null)
    {
        var options = new DbContextOptionsBuilder<GateDbContext>().UseNpgsql(_database.ConnectionString);
        if (gate is not null)
        {
            options.AddInterceptors(gate);
        }

        return new GateDbContext(options.Options);
    }
}
