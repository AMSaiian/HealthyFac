using HealthyFac.Core.Infrastructure;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Shared.Application.CurrentUser;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HealthyFac.Testing.Containers;

namespace HealthyFac.Core.IntegrationTests.Database.Data;

public sealed class CoreDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlTestDatabase _database = new();

    public ServiceProvider Services { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(DatabaseTestData.SettingsFileName, optional: false)
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [DatabaseTestData.ConnectionStringKey] = _database.ConnectionString,
                })
            .Build();

        var environment = new TestHostEnvironment();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(environment);
        services.AddSingleton<ICurrentUserProvider, AnonymousCurrentUserProvider>();
        services.AddInfrastructureServices(configuration, environment);
        Services = services.BuildServiceProvider(validateScopes: true);

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<CoreDbContext>().Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await Services.DisposeAsync();
        await _database.DisposeAsync();
    }
}
