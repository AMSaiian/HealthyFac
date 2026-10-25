using HealthyFac.Core.Application;
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

    private ServiceProvider _services = null!;

    public IServiceScopeFactory ScopeFactory => _services.GetRequiredService<IServiceScopeFactory>();


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
        services.AddApplicationServices();
        _services = services.BuildServiceProvider(validateScopes: true);

        using var scope = _services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<CoreDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<ICoreDbInitializer>().SeedRequiredDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }
}
