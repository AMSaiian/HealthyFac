using HealthyFac.Core.Application;
using HealthyFac.Core.Infrastructure;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Core.Infrastructure.Database.Initialization;
using HealthyFac.MachineSimulator.Machines;
using HealthyFac.MachineSimulator.Publishing;
using HealthyFac.MachineSimulator.Simulation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Telemetry.Infrastructure.Database;
using HealthyFac.Telemetry.Infrastructure.Database.Initialization;
using HealthyFac.Testing.Containers;
using HealthyFac.Testing.Logging;
using HealthyFac.Testing.Persistence;
using HealthyFac.Testing.Resetting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
using Npgsql;

namespace HealthyFac.MachineSimulator.IntegrationTests.Simulation.Data;

/// <summary>
/// Starts one PostgreSQL container with the Core and the Telemetry database, one Mosquitto broker, and the simulator
/// host. The Core and Telemetry use cases run in their own service providers with write access. The simulator host
/// reads both databases through read-only connections and runs on a fake clock, so a test calls each loop step by
/// hand. <see cref="ResetAsync"/> deletes the arranged rows and clears the engine.
/// </summary>
public sealed class SimulatorFixture : IAsyncLifetime, IResetBeforeEachTest
{
    private readonly PostgreSqlTestDatabase _postgres = new();
    private readonly MosquittoTestBroker _broker = new(FindFreePort());

    private ServiceProvider _core = null!;
    private ServiceProvider _telemetry = null!;
    private DatabaseResetter _coreResetter = null!;
    private DatabaseResetter _telemetryResetter = null!;
    private SimulatorFactory _factory = null!;
    private string _coreConnectionString = null!;
    private string _telemetryConnectionString = null!;
    private bool _isBrokerRunning;

    public FakeTimeProvider Clock { get; } = new(DateTimeOffset.UtcNow);

    public CapturingLoggerProvider Logs { get; } = new();

    public MachineMessageSubscriber Subscriber { get; private set; } = new();

    public MosquittoTestBroker Broker => _broker;

    public string SimulatorCoreConnectionString => ReadOnly(_coreConnectionString);

    public string SimulatorTelemetryConnectionString => ReadOnly(_telemetryConnectionString);

    public IReadOnlyDictionary<string, string?> SimulatorSettings => CreateSimulatorSettings();

    public SimulatorFactory Factory => _factory;

    public IServiceScopeFactory CoreScopeFactory =>
        new InitializationActorScopeFactory(_core.GetRequiredService<IServiceScopeFactory>(), ServiceName.Core);

    public IServiceScopeFactory TelemetryScopeFactory =>
        new InitializationActorScopeFactory(
            _telemetry.GetRequiredService<IServiceScopeFactory>(),
            ServiceName.Telemetry);

    public SimulationEngine Engine => _factory.Services.GetRequiredService<SimulationEngine>();

    public SimulationLoops Loops => _factory.Services.GetServices<IHostedService>().OfType<SimulationLoops>().Single();

    public MqttMachineMessagePublisher Publisher =>
        _factory.Services.GetRequiredService<MqttMachineMessagePublisher>();

    public TService GetSimulatorSingleton<TService>()
        where TService : class
        => _factory.Services.GetRequiredService<TService>();

    public IServiceScope CreateCoreUserScope(Guid companyId) => CreateUserScope(_core, companyId);

    public IServiceScope CreateTelemetryUserScope(Guid companyId) => CreateUserScope(_telemetry, companyId);

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _broker.StartAsync());
        _isBrokerRunning = true;

        _coreConnectionString = await CreateDatabaseAsync(SimulatorTestData.CoreDatabaseName);
        _telemetryConnectionString = await CreateDatabaseAsync(SimulatorTestData.TelemetryDatabaseName);

        _core = BuildCoreServices();
        _telemetry = BuildTelemetryServices();
        await PrepareDatabasesAsync();

        _factory = new SimulatorFactory(CreateSimulatorSettings(), Clock, Logs);
        _ = _factory.Services;
        await WaitUntilAsync(() => Publisher.IsConnected);
        await Subscriber.ConnectAsync(_broker);
    }

    /// <summary>
    /// Deletes the arranged rows, seeds the required data again, polls the state so that the engine drops every
    /// machine, and clears the received messages and the logs. A test that stopped the broker gets it back.
    /// </summary>
    public async ValueTask ResetAsync()
    {
        if (!_isBrokerRunning)
        {
            await RestartBrokerAsync();
            await WaitForConnectionAsync();
        }

        await _coreResetter.ResetAsync();
        await _telemetryResetter.ResetAsync();
        await SeedRequiredDataAsync();
        await Loops.PollStateAsync(CancellationToken.None);

        Subscriber.Clear();
        Logs.Reset();
    }

    public async Task StopBrokerAsync()
    {
        await _broker.StopAsync();
        _isBrokerRunning = false;
    }

    /// <summary>
    /// Starts the broker again and connects the test subscriber to it. The host connects again when a test advances
    /// the clock by the reconnect delay.
    /// </summary>
    public async Task RestartBrokerAsync()
    {
        await _broker.StartAsync();
        _isBrokerRunning = true;
        await Subscriber.DisposeAsync();
        Subscriber = new MachineMessageSubscriber();
        await Subscriber.ConnectAsync(_broker);
    }

    /// <summary>
    /// Advances the clock by the reconnect delay until the publisher is connected.
    /// </summary>
    public Task WaitForConnectionAsync() =>
        WaitUntilAsync(() => Publisher.IsConnected, () => Clock.Advance(SimulatorTestData.ReconnectDelay));

    public async ValueTask DisposeAsync()
    {
        await Subscriber.DisposeAsync();
        await _factory.DisposeAsync();
        await _core.DisposeAsync();
        await _telemetry.DisposeAsync();
        await _broker.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private static int FindFreePort()
    {
        using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, port: 0);
        listener.Start();

        return ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static string ReadOnly(string connectionString) =>
        new NpgsqlConnectionStringBuilder(connectionString) { Options = SimulatorTestData.ReadOnlyOptions }
            .ConnectionString;

    private static IServiceScope CreateUserScope(ServiceProvider provider, Guid companyId) =>
        provider.GetRequiredService<IServiceScopeFactory>()
            .CreateActorScope(
                new ActorContext
                {
                    Kind = ActorKind.User,
                    UserId = Guid.CreateVersion7(),
                    CompanyId = companyId,
                });

    private static IConfiguration BuildConfiguration(string file, string connectionString) =>
        new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(file, optional: false)
            .AddInMemoryCollection(
                new Dictionary<string, string?> { [SimulatorTestData.ConnectionStringKey] = connectionString })
            .Build();

    private static async Task WaitUntilAsync(Func<bool> isDone, Action? nudge = null)
    {
        var deadline = DateTime.UtcNow + SimulatorTestData.WaitLimit;
        while (!isDone() && DateTime.UtcNow < deadline)
        {
            nudge?.Invoke();
            await Task.Delay(TimeSpan.FromMilliseconds(milliseconds: 100));
        }

        isDone().Should().BeTrue();
    }

    private async Task<string> CreateDatabaseAsync(string name)
    {
        await using var connection = new NpgsqlConnection(_postgres.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE DATABASE {name}", connection);
        await command.ExecuteNonQueryAsync();

        return new NpgsqlConnectionStringBuilder(_postgres.ConnectionString) { Database = name }.ConnectionString;
    }

    private ServiceProvider BuildCoreServices()
    {
        var configuration = BuildConfiguration(SimulatorTestData.CoreSettingsFile, _coreConnectionString);
        var environment = new TestHostEnvironment();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(environment);
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddActorContext();
        services.AddApplicationServices();
        services.AddInfrastructureServices(configuration, environment);

        return services.BuildServiceProvider(validateScopes: true);
    }

    private ServiceProvider BuildTelemetryServices() =>
        TelemetryServices.Build(
            BuildConfiguration(SimulatorTestData.TelemetrySettingsFile, _telemetryConnectionString),
            new TestHostEnvironment());

    private async Task PrepareDatabasesAsync()
    {
        using var coreScope = CoreScopeFactory.CreateScope();
        var coreContext = coreScope.ServiceProvider.GetRequiredService<CoreDbContext>();
        await coreContext.Database.MigrateAsync();
        await coreScope.ServiceProvider.GetRequiredService<ICoreDbInitializer>().SeedRequiredDataAsync();
        _coreResetter = await DatabaseResetter.CreateAsync(_coreConnectionString, coreContext);

        using var telemetryScope = TelemetryScopeFactory.CreateScope();
        var telemetryContext = telemetryScope.ServiceProvider.GetRequiredService<TelemetryDbContext>();
        await telemetryContext.Database.MigrateAsync();
        await telemetryScope.ServiceProvider.GetRequiredService<ITelemetryDbInitializer>().SeedRequiredDataAsync();
        _telemetryResetter = await DatabaseResetter.CreateAsync(_telemetryConnectionString, telemetryContext);
    }

    private async Task SeedRequiredDataAsync()
    {
        using var coreScope = CoreScopeFactory.CreateScope();
        await coreScope.ServiceProvider.GetRequiredService<ICoreDbInitializer>().SeedRequiredDataAsync();

        using var telemetryScope = TelemetryScopeFactory.CreateScope();
        await telemetryScope.ServiceProvider.GetRequiredService<ITelemetryDbInitializer>().SeedRequiredDataAsync();
    }

    private Dictionary<string, string?> CreateSimulatorSettings() =>
        new()
        {
            [$"ConnectionStrings:{SimulatorDatabaseConstants.ConnectionStrings.Core}"] =
                ReadOnly(_coreConnectionString),
            [$"ConnectionStrings:{SimulatorDatabaseConstants.ConnectionStrings.Telemetry}"] =
                ReadOnly(_telemetryConnectionString),
            [key: "Mqtt:Host"] = _broker.Host,
            [key: "Mqtt:Port"] = _broker.Port.ToString(),
            [key: "Mqtt:Username"] = MosquittoTestBroker.SenderUser,
            [key: "Mqtt:Password"] = _broker.SenderPassword,
            [key: "Mqtt:ReconnectDelay"] = SimulatorTestData.ReconnectDelay.ToString(),
            [key: "Simulator:RandomSeed"] = SimulatorTestData.RandomSeed.ToString(),
        };
}
