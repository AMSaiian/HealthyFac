using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.Persistence.SoftDelete;
using HealthyFac.Testing.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace HealthyFac.Shared.IntegrationTests.Persistence.Data;

public sealed class SoftDeleteFixture : IAsyncLifetime
{
    private readonly PostgreSqlTestDatabase _database = new();
    private readonly ActorContextHolder _holder = new();

    private IDisposable? _actorLease;

    public FakeTimeProvider TimeProvider { get; } = new(SoftDeleteTestData.Now);

    public async ValueTask InitializeAsync()
    {
        await _database.StartAsync();
        _actorLease = _holder.Enter(SoftDeleteTestData.Actor);
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _actorLease?.Dispose();
        await _database.DisposeAsync();
    }

    public SoftDeleteDbContext CreateDbContext()
    {
        var currentUser = new ActorCurrentUserProvider(_holder);
        var options = new DbContextOptionsBuilder<SoftDeleteDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .AddInterceptors(new SoftDeleteInterceptor(TimeProvider, currentUser))
            .Options;

        return new SoftDeleteDbContext(options);
    }
}
