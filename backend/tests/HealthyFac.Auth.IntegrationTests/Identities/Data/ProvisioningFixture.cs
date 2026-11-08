using HealthyFac.Auth.Application;
using HealthyFac.Auth.Infrastructure;
using HealthyFac.Auth.Infrastructure.Database;
using HealthyFac.Auth.IntegrationTests.Keycloak.Data;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.Messaging.Transport;
using HealthyFac.Testing.Containers;
using HealthyFac.Testing.Persistence;
using HealthyFac.Testing.Resetting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Serialization.Json;

namespace HealthyFac.Auth.IntegrationTests.Identities.Data;

/// <summary>
/// Runs the auth host against real PostgreSQL, RabbitMQ, Redis, Keycloak, and Mailpit containers. The inbox
/// processor runs as a hosted service. The outbox processor does not run, so the rows of the handler stay pending
/// and a test reads them. <see cref="PublishAsync"/> plays the core service.
/// </summary>
/// <remarks>
/// One instance serves every test class of <see cref="KeycloakCollection"/>. The host uses the Redis container of
/// <see cref="Keycloak"/>, so the run starts one Redis container.
/// </remarks>
public sealed class ProvisioningFixture : IAsyncLifetime, IResetBeforeEachTest
{
    private readonly PostgreSqlTestDatabase _database = new();
    private readonly RabbitMqTestBroker _broker = new();

    private IHost _host = null!;
    private IBus _publisher = null!;
    private DatabaseResetter _resetter = null!;

    public KeycloakAdminFixture Keycloak { get; } = new();

    public IServiceScopeFactory ScopeFactory => _host.Services.GetRequiredService<IServiceScopeFactory>();

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(
            _database.StartAsync(),
            _broker.StartAsync(),
            Keycloak.InitializeAsync().AsTask());

        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Configuration.AddInMemoryCollection(Keycloak.Settings);
        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                [ProvisioningTestData.DatabaseConnectionStringKey] = _database.ConnectionString,
                [ProvisioningTestData.BrokerConnectionStringKey] = _broker.ConnectionString,
                [ProvisioningTestData.QueueNameKey] = ProvisioningTestData.QueueName,
                [ProvisioningTestData.RedisConnectionStringKey] = Keycloak.Redis.ConnectionString,
                [ProvisioningTestData.PollingIntervalKey] = ProvisioningTestData.PollingIntervalMilliseconds,
                [ProvisioningTestData.ServiceNameKey] = ServiceName.Auth.Value,
            });
        builder.Services.AddApplicationServices();
        builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);

        var outboxProcessors = builder.Services
            .Where(descriptor => descriptor.ImplementationType?.Name == ProvisioningTestData.OutboxProcessorTypeName)
            .ToList();
        foreach (var descriptor in outboxProcessors)
        {
            builder.Services.Remove(descriptor);
        }

        _host = builder.Build();
        using (var scope = _host.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            await dbContext.Database.MigrateAsync();
            _resetter = await DatabaseResetter.CreateAsync(_database.ConnectionString, dbContext);
        }

        await _host.StartAsync();

        _publisher = Configure.OneWayClient()
            .Transport(transport => transport.UseRabbitMqAsOneWayClient(_broker.ConnectionString))
            .Serialization(serialization => serialization.UseSystemTextJson())
            .Start();
    }

    /// <summary>
    /// Empties the input queue, deletes every row of the auth database, and resets Keycloak and Redis.
    /// </summary>
    public async ValueTask ResetAsync()
    {
        await _broker.PurgeQueuesAsync(ProvisioningTestData.QueueName);
        await _resetter.ResetAsync();
        await Keycloak.ResetAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _publisher.Dispose();
        await _host.StopAsync();
        _host.Dispose();
        await Keycloak.DisposeAsync();
        await _broker.DisposeAsync();
        await _database.DisposeAsync();
    }

    public IServiceScope CreateActorScope(Guid companyId)
        => ScopeFactory.CreateActorScope(ServiceActor.DatabaseInitialization.For(ServiceName.Auth, companyId));

    public Task PublishAsync(string topic, string payload, Guid messageId, IReadOnlyDictionary<string, string> headers)
    {
        var allHeaders = new Dictionary<string, string>(headers)
        {
            [Rebus.Messages.Headers.MessageId] = messageId.ToString(),
        };

        return _publisher.Advanced.Topics.Publish(
            topic,
            new IntegrationEnvelope { Type = topic, Payload = payload },
            allHeaders);
    }
}
