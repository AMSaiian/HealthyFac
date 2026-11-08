using HealthyFac.Testing.Containers;
using HealthyFac.Testing.Persistence;
using HealthyFac.Testing.Resetting;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.IntegrationTests.Concurrency.Data;

public sealed class GateDatabaseFixture : IAsyncLifetime, IResetBeforeEachTest
{
    private readonly PostgreSqlTestDatabase _database = new();

    private DatabaseResetter _resetter = null!;

    public async ValueTask InitializeAsync()
    {
        await _database.StartAsync();

        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        _resetter = await DatabaseResetter.CreateAsync(_database.ConnectionString, context);
    }

    public async ValueTask ResetAsync() => await _resetter.ResetAsync();

    public ValueTask DisposeAsync() => _database.DisposeAsync();

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
