using HealthyFac.Shared.Application.Time;
using HealthyFac.Shared.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HealthyFac.Testing.Containers;

namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlTestDatabase _database = new();

    public ServiceProvider Services { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        var services = new ServiceCollection();
        services.AddSingleton<IDateProvider, FixedDateProvider>();
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
