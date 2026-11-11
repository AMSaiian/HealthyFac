using HealthyFac.Insights.Application;
using HealthyFac.Insights.Application.Files;
using HealthyFac.Insights.Infrastructure;
using HealthyFac.Insights.Infrastructure.Database;
using HealthyFac.Insights.Infrastructure.MetricUnits;
using HealthyFac.Insights.IntegrationTests.Ingest.Data;
using HealthyFac.Insights.IntegrationTests.MetricUnits.Data;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Testing.Containers;
using HealthyFac.Testing.Persistence;
using HealthyFac.Testing.Resetting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Insights.IntegrationTests.Database.Data;

/// <summary>
/// The collection fixture of the Insights integration tests. It owns PostgreSQL (with pgvector), RabbitMQ, and Redis,
/// builds the service provider of the real application and infrastructure registrations, and resets the database and
/// Redis before each test.
/// </summary>
public sealed class InsightsFixture : IAsyncLifetime, IResetBeforeEachTest
{
    private readonly PostgreSqlTestDatabase _database = new();
    private readonly RabbitMqTestBroker _broker = new();
    private readonly RedisTestServer _redis = new();
    private readonly Action<IDictionary<string, string?>>? _configureSettings;
    private readonly Action<IServiceCollection>? _configureServices;

    private ServiceProvider _services = null!;
    private DatabaseResetter _resetter = null!;

    public InsightsFixture()
    {
    }

    /// <summary>
    /// Creates a fixture that adds settings and services to the defaults. A test class with its own fixture type uses
    /// it, for example to replace a service with a test double.
    /// </summary>
    /// <param name="configureSettings">Adds settings after the connection strings are set.</param>
    /// <param name="configureServices">Adds or replaces services after the defaults.</param>
    internal InsightsFixture(
        Action<IDictionary<string, string?>>? configureSettings,
        Action<IServiceCollection>? configureServices
    )
    {
        _configureSettings = configureSettings;
        _configureServices = configureServices;
    }

    public ConcurrentSaveGate SaveGate { get; } = new();

    public SaveHook SaveHook { get; } = new();

    public FakeFileCatalog Files { get; } = new();

    public FakeMetricUnitSource Units { get; } = new();

    public LogCapture Logs { get; } = new();

    public IngestFakes Ingest { get; } = new();

    public IServiceScopeFactory ScopeFactory =>
        new InitializationActorScopeFactory(_services.GetRequiredService<IServiceScopeFactory>());

    public IServiceScope CreateActorScope(ActorContext actor)
        => _services.GetRequiredService<IServiceScopeFactory>().CreateActorScope(actor);

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
        await Task.WhenAll(_database.StartAsync(), _broker.StartAsync(), _redis.StartAsync());

        var settings = new Dictionary<string, string?>
        {
            [DatabaseTestData.ConnectionStringKey] = _database.ConnectionString,
            [DatabaseTestData.RabbitMqConnectionStringKey] = _broker.ConnectionString,
            [DatabaseTestData.RedisConnectionStringKey] = _redis.ConnectionString,
        };
        _configureSettings?.Invoke(settings);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(DatabaseTestData.SettingsFileName, optional: false)
            .AddInMemoryCollection(settings)
            .Build();

        var environment = new TestHostEnvironment();
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(Logs));
        services.AddMetrics();
        services.AddSingleton<IHostEnvironment>(environment);
        services.AddActorContext();
        services.AddApplicationServices();
        services.AddInfrastructureServices(configuration, environment);
        services.ConfigureDbContext<InsightsDbContext>(options => options.AddInterceptors(SaveGate, SaveHook));
        services.AddSingleton(Files);
        services.AddSingleton<IFileCatalog>(Files);
        services.AddSingleton<IMetricUnitSource>(Units);
        Ingest.Register(services);
        _configureServices?.Invoke(services);

        _services = services.BuildServiceProvider(validateScopes: true);

        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InsightsDbContext>();
        await dbContext.Database.MigrateAsync();
        _resetter = await DatabaseResetter.CreateAsync(_database.ConnectionString, dbContext);
    }

    /// <summary>
    /// Stops the Redis container, so a test simulates a Redis outage. Use it only in a collection of its own.
    /// </summary>
    public Task StopRedisAsync() => _redis.StopAsync();

    /// <summary>
    /// Deletes every row and flushes Redis.
    /// </summary>
    public async ValueTask ResetAsync()
    {
        Files.Clear();
        Units.Reset();
        Logs.Clear();
        Ingest.Reset();
        SaveHook.Clear();
        await _resetter.ResetAsync();
        await _redis.FlushAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
        await _broker.DisposeAsync();
        await _redis.DisposeAsync();
    }
}
