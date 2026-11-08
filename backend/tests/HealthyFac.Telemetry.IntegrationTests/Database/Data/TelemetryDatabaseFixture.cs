using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Infrastructure;
using HealthyFac.Telemetry.Infrastructure.Database;
using HealthyFac.Telemetry.Infrastructure.Database.Initialization;
using HealthyFac.Testing.Containers;
using HealthyFac.Testing.Persistence;
using HealthyFac.Testing.Resetting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HealthyFac.Telemetry.IntegrationTests.Database.Data;

public sealed class TelemetryDatabaseFixture : IAsyncLifetime, IResetBeforeEachTest
{
    private readonly PostgreSqlTestDatabase _database = new();
    private readonly Action<IDictionary<string, string?>>? _configureSettings;
    private readonly Action<IServiceCollection>? _configureServices;

    private ServiceProvider _services = null!;
    private DatabaseResetter _resetter = null!;

    public TelemetryDatabaseFixture()
    {
    }

    /// <summary>
    /// Creates a fixture that adds settings and services to the defaults. A test class with its own fixture type uses
    /// it, for example to run the MQTT consumer against a broker.
    /// </summary>
    /// <param name="configureSettings">Adds settings after the connection string is set.</param>
    /// <param name="configureServices">Adds or replaces services after the defaults.</param>
    internal TelemetryDatabaseFixture(
        Action<IDictionary<string, string?>>? configureSettings,
        Action<IServiceCollection>? configureServices
    )
    {
        _configureSettings = configureSettings;
        _configureServices = configureServices;
    }

    public ConcurrentSaveGate SaveGate { get; } = new();

    public IServiceScopeFactory ScopeFactory =>
        new InitializationActorScopeFactory(_services.GetRequiredService<IServiceScopeFactory>());

    public IServiceScope CreateActorScope(ActorContext actor)
        => _services.GetRequiredService<IServiceScopeFactory>().CreateActorScope(actor);

    /// <summary>
    /// Returns the registered hosted service of a type. The test starts and stops it, because the test provider does
    /// not run a host.
    /// </summary>
    /// <typeparam name="THostedService">The type of the hosted service.</typeparam>
    /// <returns>The hosted service.</returns>
    public THostedService GetHostedService<THostedService>()
        where THostedService : class, IHostedService
        => _services.GetRequiredService<THostedService>();

    /// <summary>
    /// Returns a singleton that a test reads, for example the state of a consumer.
    /// </summary>
    /// <typeparam name="TService">The type of the singleton.</typeparam>
    /// <returns>The service.</returns>
    public TService GetSingleton<TService>()
        where TService : class
        => _services.GetRequiredService<TService>();

    public async ValueTask InitializeAsync()
    {
        await _database.StartAsync();

        var settings = new Dictionary<string, string?>
        {
            [DatabaseTestData.ConnectionStringKey] = _database.ConnectionString,
        };
        _configureSettings?.Invoke(settings);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(DatabaseTestData.SettingsFileName, optional: false)
            .AddInMemoryCollection(settings)
            .Build();

        var environment = new TestHostEnvironment();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(environment);
        services.AddActorContext();
        services.AddApplicationServices();
        services.AddInfrastructureServices(configuration, environment);
        services.ConfigureDbContext<TelemetryDbContext>(options => options.AddInterceptors(SaveGate));
        _configureServices?.Invoke(services);

        _services = services.BuildServiceProvider(validateScopes: true);

        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();
        await dbContext.Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<ITelemetryDbInitializer>().SeedRequiredDataAsync();
        _resetter = await DatabaseResetter.CreateAsync(_database.ConnectionString, dbContext);
    }

    /// <summary>
    /// Deletes every row and seeds the required data again, because a test may change the unit catalog.
    /// </summary>
    public async ValueTask ResetAsync()
    {
        await _resetter.ResetAsync();

        using var scope = ScopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ITelemetryDbInitializer>().SeedRequiredDataAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }
}
