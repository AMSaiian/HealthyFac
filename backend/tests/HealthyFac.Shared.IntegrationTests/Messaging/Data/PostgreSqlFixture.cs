using HealthyFac.Shared.Application.Time;
using HealthyFac.Shared.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder(InboxTestData.PostgresImage).Build();

    public ServiceProvider Services { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var services = new ServiceCollection();
        services.AddSingleton<IDateProvider, FixedDateProvider>();
        services.AddMessagingDbContext<ITestServiceDbContext, TestServiceDbContext>(
            (_, options) => options.UseNpgsql(_container.GetConnectionString()));
        Services = services.BuildServiceProvider(validateScopes: true);

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<TestServiceDbContext>().Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await Services.DisposeAsync();
        await _container.DisposeAsync();
    }
}
