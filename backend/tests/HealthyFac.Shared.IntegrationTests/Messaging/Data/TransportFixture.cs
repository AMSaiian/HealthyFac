using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.Messaging;
using HealthyFac.Shared.Infrastructure.Messaging.Processing;
using HealthyFac.Shared.Infrastructure.Messaging.Transport;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Testing.Containers;
using HealthyFac.Testing.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Serialization.Json;

namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

/// <summary>
/// Runs the transport of a test service against real PostgreSQL and RabbitMQ containers. The host starts the
/// bus and the subscription, and <see cref="PublishAsync"/> plays the part of another service.
/// </summary>
public class TransportFixture : IAsyncLifetime
{
    private readonly PostgreSqlTestDatabase _database = new();

    private readonly RabbitMqTestBroker _broker = new();

    private IHost _host = null!;

    private IBus _publisher = null!;

    public string BrokerConnectionString => _broker.ConnectionString;

    public HandlerRecorder Recorder { get; } = new();

    public CapturingLoggerProvider Logs { get; } = new();

    public async Task InitializeAsync()
    {
        await _database.StartAsync();
        await _broker.StartAsync();

        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                [TransportTestData.BrokerConnectionStringKey] = _broker.ConnectionString,
                [TransportTestData.QueueNameKey] = TransportTestData.QueueName,
                [TransportTestData.PollingIntervalKey] = TransportTestData.PollingIntervalMilliseconds,
            });
        builder.Logging.AddProvider(Logs);
        builder.Services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(InboxTestData.Now)));
        builder.Services.AddActorContext();
        builder.Services.AddScoped<AuditableEntityInterceptor>();
        builder.Services.AddMessagingDbContext<ITestServiceDbContext, TestServiceDbContext>((provider, options) =>
        {
            options.UseNpgsql(_database.ConnectionString)
                .AddInterceptors(provider.GetRequiredService<AuditableEntityInterceptor>());
            ConfigureDbContext(options, _database.ConnectionString);
        });
        builder.Services.AddMessagingTransport(builder.Configuration);
        builder.Services.AddScoped<OutboxMessagePublisher>();
        builder.Services.AddScoped<InboxMessageRunner>();
        builder.Services.AddSingleton(Recorder);
        builder.Services.AddUseCases(typeof(TestPing).Assembly);
        builder.Services.AddIntegrationMessage<TestPing>();
        builder.Services.AddIntegrationMessage<TestRecorded>();
        builder.Services.AddIntegrationMessage<TestFailing>();
        ConfigureServices(builder.Services);

        _host = builder.Build();
        using (var scope = _host.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<TestServiceDbContext>().Database.EnsureCreatedAsync();
        }

        await _host.StartAsync();

        _publisher = Configure.OneWayClient()
            .Transport(transport => transport.UseRabbitMqAsOneWayClient(_broker.ConnectionString))
            .Serialization(serialization => serialization.UseSystemTextJson())
            .Start();
    }

    public async Task DisposeAsync()
    {
        _publisher.Dispose();
        await _host.StopAsync();
        _host.Dispose();
        await _broker.DisposeAsync();
        await _database.DisposeAsync();
    }

    /// <summary>
    /// Builds a service with the dependencies of the test host, so a test can run a processor by hand instead of
    /// waiting for a hosted service.
    /// </summary>
    public T Create<T>(params object[] parameters) => ActivatorUtilities.CreateInstance<T>(_host.Services, parameters);

    /// <summary>
    /// Lets a derived fixture add services before the host builds, for example the hosted processors.
    /// </summary>
    /// <param name="services">The service collection of the test host.</param>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    /// <summary>
    /// Lets a derived fixture change the options of the test service context, for example to add a retry
    /// strategy or an interceptor that simulates a database failure.
    /// </summary>
    /// <param name="options">The options builder of the context. It already holds the Npgsql provider.</param>
    /// <param name="connectionString">The connection string of the test database.</param>
    protected virtual void ConfigureDbContext(DbContextOptionsBuilder options, string connectionString)
    {
    }

    public Task StopBrokerAsync() => _broker.StopAsync();

    public IServiceScope CreateScope(ActorContext? actor = null)
    {
        return _host.Services.GetRequiredService<IServiceScopeFactory>()
            .CreateActorScope(actor ?? SystemActor.Create());
    }

    public async Task<InboxMessage?> WaitForInboxMessageAsync(Guid messageId)
    {
        using var cancellation = new CancellationTokenSource(TransportTestData.WaitTimeout);
        while (!cancellation.IsCancellationRequested)
        {
            using var scope = CreateScope();
            var message = await scope.ServiceProvider.GetRequiredService<TestServiceDbContext>()
                .InboxMessages
                .AsNoTracking()
                .SingleOrDefaultAsync(stored => stored.Id == messageId);
            if (message is not null)
            {
                return message;
            }

            await Task.Delay(TransportTestData.PollDelay);
        }

        return null;
    }

    public async Task<int> CountInboxMessagesAsync(Guid messageId)
    {
        using var scope = CreateScope();

        return await scope.ServiceProvider.GetRequiredService<TestServiceDbContext>()
            .InboxMessages
            .CountAsync(stored => stored.Id == messageId);
    }

    /// <summary>
    /// Waits until the input queue holds no ready message and stays quiet for a short period, so a late
    /// duplicate delivery has reached the inbox before a test counts rows.
    /// </summary>
    public async Task WaitForQueueToSettleAsync()
    {
        using var cancellation = new CancellationTokenSource(TransportTestData.WaitTimeout);
        while (!cancellation.IsCancellationRequested)
        {
            var readyCount = await RabbitMqQueueReader.CountReadyMessagesAsync(
                _broker.ConnectionString,
                TransportTestData.QueueName);
            if (readyCount == 0)
            {
                break;
            }

            await Task.Delay(TransportTestData.PollDelay);
        }

        await Task.Delay(TransportTestData.QuietPeriod);
    }

    public Task PublishAsync(
        string topic,
        string payload,
        Guid messageId,
        IReadOnlyDictionary<string, string> headers
    )
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
