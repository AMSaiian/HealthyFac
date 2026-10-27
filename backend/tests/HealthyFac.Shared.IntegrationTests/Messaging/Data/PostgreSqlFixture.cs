using Microsoft.Extensions.Time.Testing;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HealthyFac.Testing.Containers;

namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlTestDatabase _database = new();

    public ServiceProvider Services { get; private set; } = null!;

    public string ConnectionString => _database.ConnectionString;

    /// <summary>
    /// Creates a scope that holds <paramref name="actor"/>, or the system actor when the argument is null.
    /// </summary>
    public IServiceScope CreateScope(ActorContext? actor = null)
    {
        return Services.GetRequiredService<IServiceScopeFactory>().CreateActorScope(actor ?? SystemActor.Create());
    }

    public IServiceScope CreateScopeWithoutActor() => Services.CreateScope();

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(InboxTestData.Now)));
        services.AddActorContext();
        services.AddMessagingDbContext<ITestServiceDbContext, TestServiceDbContext>((_, options) =>
            options.UseNpgsql(_database.ConnectionString));
        Services = services.BuildServiceProvider(validateScopes: true);

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<TestServiceDbContext>().Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await Services.DisposeAsync();
        await _database.DisposeAsync();
    }
}
