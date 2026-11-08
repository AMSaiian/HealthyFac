using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.Messaging;
using HealthyFac.Testing.Containers;
using HealthyFac.Testing.Persistence;
using HealthyFac.Testing.Resetting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

public sealed class PostgreSqlFixture : IAsyncLifetime, IResetBeforeEachTest
{
    private readonly PostgreSqlTestDatabase _database = new();

    private DatabaseResetter _resetter = null!;

    public ServiceProvider Services { get; private set; } = null!;

    public string ConnectionString => _database.ConnectionString;

    /// <summary>
    /// Creates a scope that holds <paramref name="actor"/>, or the database initialization actor of the
    /// core service when the argument is null.
    /// </summary>
    public IServiceScope CreateScope(ActorContext? actor = null) =>
        Services.GetRequiredService<IServiceScopeFactory>()
            .CreateActorScope(actor ?? ServiceActor.DatabaseInitialization.For(ServiceName.Core));

    public IServiceScope CreateScopeWithoutActor() => Services.CreateScope();

    public async ValueTask InitializeAsync()
    {
        await _database.StartAsync();

        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(InboxTestData.Now)));
        services.AddActorContext();
        services.AddMessagingMetrics();
        services.AddMessagingDbContext<ITestServiceDbContext, TestServiceDbContext>((_, options) =>
            options.UseNpgsql(_database.ConnectionString));
        Services = services.BuildServiceProvider(validateScopes: true);

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestServiceDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
        _resetter = await DatabaseResetter.CreateAsync(_database.ConnectionString, dbContext);
    }

    public async ValueTask ResetAsync() => await _resetter.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await Services.DisposeAsync();
        await _database.DisposeAsync();
    }
}
