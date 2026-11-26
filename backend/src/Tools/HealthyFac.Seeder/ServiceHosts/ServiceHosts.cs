using HealthyFac.Core.Application.Employees.SendEmployeeInviteEmail;
using HealthyFac.Seeder.Relay;
using HealthyFac.Seeder.Timeline;
using HealthyFac.Shared.Application.BackgroundQueues;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.CurrentUser.ServiceIdentity;
using HealthyFac.Shared.Infrastructure.IdEncryption;
using HealthyFac.Shared.Infrastructure.ServiceTokens;
using HealthyFac.Shared.Infrastructure.ServiceTokens.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Rebus.Bus;
using AuthApplication = HealthyFac.Auth.Application.ConfigureServices;
using AuthInfrastructure = HealthyFac.Auth.Infrastructure.ConfigureServices;
using AuthDatabaseConstants = HealthyFac.Auth.Infrastructure.Database.DatabaseConstants;
using AuthInitialization = HealthyFac.Auth.Infrastructure.Database.Initialization.DatabaseInitializationExtensions;
using CoreApplication = HealthyFac.Core.Application.ConfigureServices;
using CoreInfrastructure = HealthyFac.Core.Infrastructure.ConfigureServices;
using CoreDatabaseConstants = HealthyFac.Core.Infrastructure.Database.DatabaseConstants;
using CoreInitialization = HealthyFac.Core.Infrastructure.Database.Initialization.DatabaseInitializationExtensions;
using DocumentApplication = HealthyFac.Document.Application.ConfigureServices;
using DocumentInfrastructure = HealthyFac.Document.Infrastructure.ConfigureServices;
using DocumentDatabaseConstants = HealthyFac.Document.Infrastructure.Database.DatabaseConstants;
using DocumentInitialization =
    HealthyFac.Document.Infrastructure.Database.Initialization.DatabaseInitializationExtensions;
using InsightsApplication = HealthyFac.Insights.Application.ConfigureServices;
using InsightsInfrastructure = HealthyFac.Insights.Infrastructure.ConfigureServices;
using InsightsDatabaseConstants = HealthyFac.Insights.Infrastructure.Database.DatabaseConstants;
using InsightsInitialization =
    HealthyFac.Insights.Infrastructure.Database.Initialization.DatabaseInitializationExtensions;
using TelemetryApplication = HealthyFac.Telemetry.Application.ConfigureServices;
using TelemetryInfrastructure = HealthyFac.Telemetry.Infrastructure.ConfigureServices;
using TelemetryDatabaseConstants = HealthyFac.Telemetry.Infrastructure.Database.DatabaseConstants;
using TelemetryInitialization =
    HealthyFac.Telemetry.Infrastructure.Database.Initialization.DatabaseInitializationExtensions;

namespace HealthyFac.Seeder.ServiceHosts;

/// <summary>
/// The 5 service hosts of the seed job. Each host reads the settings of its Web project, the deployment file of its
/// service, and the keys <c>Services:&lt;Name&gt;:*</c> of the root configuration (the environment variables
/// <c>Services__&lt;Name&gt;__*</c>), in this order, so a later source overrides an earlier one. The settings file
/// of the environment (<c>appsettings.Development.json</c>) comes after <c>appsettings.json</c>, as in the services.
/// </summary>
public sealed class ServiceHosts : IAsyncDisposable
{
    private const string SettingsFileName = "appsettings.json";
    private const string DeploymentFileName = "appsettings.Deployment.json";
    private const string ServicesDirectory = "services";
    private const string SectionPrefix = "Services";
    private const string RebusAssemblyPrefix = "Rebus";

    private ServiceHosts(
        ServiceHost core,
        ServiceHost telemetry,
        ServiceHost document,
        ServiceHost insights,
        ServiceHost auth
    )
    {
        Core = core;
        Telemetry = telemetry;
        Document = document;
        Insights = insights;
        Auth = auth;
        All = [core, telemetry, document, insights, auth];
    }

    public ServiceHost Core { get; }

    public ServiceHost Telemetry { get; }

    public ServiceHost Document { get; }

    public ServiceHost Insights { get; }

    public ServiceHost Auth { get; }

    public IReadOnlyList<ServiceHost> All { get; }

    /// <summary>Builds the 5 hosts. No host starts a hosted service.</summary>
    /// <param name="rootConfiguration">The configuration that holds the <c>Services:&lt;Name&gt;:*</c> keys.</param>
    /// <param name="clock">The clock that every host uses as its <see cref="TimeProvider"/>.</param>
    /// <param name="baseDirectory">The directory that holds the <c>services</c> folder.</param>
    /// <returns>The hosts.</returns>
    public static ServiceHosts Create(IConfiguration rootConfiguration, SeedClock clock, string baseDirectory) =>
        new(
            Build(
                ServiceName.Core,
                CoreDatabaseConstants.ConnectionStringName,
                rootConfiguration,
                clock,
                baseDirectory,
                CoreApplication.AddApplicationServices,
                CoreInfrastructure.AddInfrastructureServices,
                CoreInitialization.InitializeDatabaseAsync,
                (services, _) => services.AddScoped<IEmployeeInviteEmailBuilder, SeedInviteEmailBuilder>()),
            Build(
                ServiceName.Telemetry,
                TelemetryDatabaseConstants.ConnectionStringName,
                rootConfiguration,
                clock,
                baseDirectory,
                TelemetryApplication.AddApplicationServices,
                TelemetryInfrastructure.AddInfrastructureServices,
                TelemetryInitialization.InitializeDatabaseAsync),
            Build(
                ServiceName.Document,
                DocumentDatabaseConstants.ConnectionStringName,
                rootConfiguration,
                clock,
                baseDirectory,
                DocumentApplication.AddApplicationServices,
                DocumentInfrastructure.AddInfrastructureServices,
                DocumentInitialization.InitializeDatabaseAsync,
                (services, configuration) => services.AddIdEncryptor(configuration)),
            Build(
                ServiceName.Insights,
                InsightsDatabaseConstants.ConnectionStringName,
                rootConfiguration,
                clock,
                baseDirectory,
                InsightsApplication.AddApplicationServices,
                InsightsInfrastructure.AddInfrastructureServices,
                InsightsInitialization.InitializeDatabaseAsync,
                (services, configuration) => services.AddIdEncryptor(configuration)),
            Build(
                ServiceName.Auth,
                AuthDatabaseConstants.ConnectionStringName,
                rootConfiguration,
                clock,
                baseDirectory,
                AuthApplication.AddApplicationServices,
                AuthInfrastructure.AddInfrastructureServices,
                AuthInitialization.InitializeDatabaseAsync));

    /// <summary>Creates the relay over the databases of the 5 hosts.</summary>
    /// <returns>The relay.</returns>
    public MessageRelay CreateRelay() =>
        new([.. All.Select(host => (IRelayEndpoint)new DatabaseRelayEndpoint(host))]);

    public async ValueTask DisposeAsync()
    {
        foreach (var host in All)
        {
            await host.DisposeAsync();
        }
    }

    /// <summary>
    /// Removes every registration whose service type or implementation type comes from a Rebus assembly. The shared
    /// <c>Add...</c> methods register Rebus, and a bus build alone declares the broker queues. The job never sends
    /// through the broker, so no seed step needs a Rebus type.
    /// </summary>
    /// <param name="services">The registrations of the host, after the infrastructure registrations.</param>
    private static void RemoveRebusRegistrations(IServiceCollection services)
    {
        var rebusDescriptors = services.Where(IsFromRebusAssembly).ToList();

        foreach (var descriptor in rebusDescriptors)
        {
            services.Remove(descriptor);
        }
    }

    private static bool IsFromRebusAssembly(ServiceDescriptor descriptor) =>
        new[] { descriptor.ServiceType, descriptor.ImplementationType, descriptor.ImplementationInstance?.GetType() }
            .Any(type => type?.Assembly.GetName()
                    .Name?.StartsWith(
                        RebusAssemblyPrefix,
                        StringComparison.Ordinal)
                == true);

    /// <summary>
    /// Gives the service token parts the system clock. The seed clock moves minutes or days at each event, while the
    /// token lifetime and the lock timeout are real time: on the seed clock a token would count as expired at once,
    /// or a lock wait could never end. Every other part keeps the seed clock. A host without service tokens is kept.
    /// </summary>
    /// <param name="services">The registrations of the host, after the infrastructure registrations.</param>
    private static void UseRealClockForServiceTokens(IServiceCollection services)
    {
        if (!services.Any(descriptor => descriptor.ServiceType == typeof(TokenStore)))
        {
            return;
        }

        services.Replace(
            ServiceDescriptor.Singleton(provider => new TokenEndpointClient(
                provider.GetRequiredService<IHttpClientFactory>(),
                provider.GetRequiredService<IOptions<ServiceTokenOptions>>(),
                TimeProvider.System,
                Random.Shared)));

        services.Replace(
            ServiceDescriptor.Singleton(provider => new TokenStore(
                provider.GetRequiredService<ITokenCacheStorage>(),
                TimeProvider.System,
                provider.GetRequiredService<IOptions<ServiceTokenOptions>>(),
                provider.GetRequiredService<IOptions<ServiceOptions>>(),
                provider.GetRequiredService<ILogger<TokenStore>>())));

        services.Replace(
            ServiceDescriptor.Singleton(provider => new ServiceTokenCache(
                provider.GetRequiredService<TokenEndpointClient>(),
                provider.GetRequiredService<TokenStore>(),
                provider.GetRequiredService<IOptions<ServiceOptions>>())));
    }

    private static IConfiguration NameDatabaseSessions(IConfiguration configuration, string connectionStringName)
    {
        var connectionStringKey = ConfigurationPath.Combine(
            SeederProcess.ConnectionStringsSection, connectionStringName);

        var connectionString = configuration[connectionStringKey];

        if (string.IsNullOrEmpty(connectionString))
        {
            return configuration;
        }

        var named = new NpgsqlConnectionStringBuilder(connectionString)
        {
            ApplicationName = SeederProcess.ApplicationName,
        }.ConnectionString;

        return new ConfigurationBuilder()
            .AddConfiguration(configuration)
            .AddInMemoryCollection(new Dictionary<string, string?> { [connectionStringKey] = named })
            .Build();
    }

    private static ServiceHost Build(
        ServiceName name,
        string connectionStringName,
        IConfiguration rootConfiguration,
        SeedClock clock,
        string baseDirectory,
        Func<IServiceCollection, IServiceCollection> addApplication,
        Func<IServiceCollection, IConfiguration, IHostEnvironment, IServiceCollection> addInfrastructure,
        Func<IServiceProvider, CancellationToken, Task> initialize,
        Action<IServiceCollection, IConfiguration>? addExtras = null
    )
    {
        var environment = new SeederHostEnvironment { ContentRootPath = baseDirectory };

        var built = new ConfigurationBuilder()
            .SetBasePath(baseDirectory)
            .AddJsonFile(Path.Combine(ServicesDirectory, name.Value, SettingsFileName), optional: false)
            .AddJsonFile(
                Path.Combine(ServicesDirectory, name.Value, $"appsettings.{environment.EnvironmentName}.json"),
                optional: false)
            .AddJsonFile(Path.Combine(ServicesDirectory, name.Value, DeploymentFileName), optional: true)
            .AddConfiguration(rootConfiguration.GetSection($"{SectionPrefix}:{name.Value}"))
            .Build();

        var configuration = NameDatabaseSessions(built, connectionStringName);

        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(clock);
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(environment);
        services.AddActorContext();
        addExtras?.Invoke(services, configuration);
        addApplication(services);
        addInfrastructure(services, configuration, environment);
        RemoveRebusRegistrations(services);
        services.RemoveAll<IHostedService>();
        services.Replace(ServiceDescriptor.Singleton<IBus, NoMessageBus>());
        services.Replace(ServiceDescriptor.Singleton<IBackgroundQueue<RealtimePush>, DiscardingRealtimeQueue>());
        UseRealClockForServiceTokens(services);

        var provider = services.BuildServiceProvider(validateScopes: true);

        try
        {
            provider.GetService<IStartupValidator>()?.Validate();
        }
        catch (OptionsValidationException exception)
        {
            throw new InvalidOperationException(ServiceHostErrors.HostBuildFailed(name.Value, exception.Message));
        }

        return new ServiceHost(name, provider, [.. services], initialize);
    }
}
