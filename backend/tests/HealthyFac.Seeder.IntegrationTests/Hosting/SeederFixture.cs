using HealthyFac.Auth.Infrastructure.Database;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Document.Infrastructure.Database;
using HealthyFac.Insights.Infrastructure.Database;
using HealthyFac.Seeder.Accounts;
using HealthyFac.Seeder.ServiceHosts;
using HealthyFac.Seeder.Timeline;
using HealthyFac.Telemetry.Infrastructure.Database;
using HealthyFac.Testing.Containers;
using HealthyFac.Testing.Persistence;
using HealthyFac.Testing.Resetting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SeederHosts = HealthyFac.Seeder.ServiceHosts.ServiceHosts;

namespace HealthyFac.Seeder.IntegrationTests.Hosting;

/// <summary>
/// The collection fixture of the Seeder integration tests. It owns one PostgreSQL server with 5 databases, Azurite,
/// Mailpit, and Redis, and builds the 5 service hosts with the seed clock. The tables start empty: a test calls
/// <see cref="InitializeHostsAsync"/> when it needs the schemas, and <see cref="ResetAsync"/> deletes every row.
/// </summary>
public sealed class SeederFixture : IAsyncLifetime, IResetBeforeEachTest
{
    private readonly PostgreSqlTestDatabase _database = new();
    private readonly AzuriteTestStorage _storage = new();
    private readonly MailpitTestServer _mailpit = new();
    private readonly RedisTestServer _redis = new();
    private readonly KeycloakTestServer _keycloak = new();
    private readonly List<Guid> _organizations = [];

    private SeederHosts _hosts = null!;
    private IReadOnlyList<DatabaseResetter>? _resetters;
    private int _momentIndex;

    public SeedClock Clock { get; } = new();

    /// <summary>
    /// Enters the next moment of the shared clock, one minute after the last one, so the moments of all tests grow.
    /// </summary>
    /// <param name="item">The item name for the clock error.</param>
    /// <returns>The entered moment.</returns>
    public DateTimeOffset EnterNextMoment(string item)
    {
        var moment = SeederTestData.FirstMoment.AddMinutes(Interlocked.Increment(ref _momentIndex));
        Clock.Enter(moment, item);

        return moment;
    }

    public SeederHosts Hosts => _hosts;

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(
            _database.StartAsync(),
            _storage.StartAsync(),
            _mailpit.StartAsync(),
            _redis.StartAsync(),
            _keycloak.StartAsync(SeederTestData.RealmEnvironment));

        _hosts = SeederHosts.Create(CreateConfiguration(CreateSettings()), Clock, AppContext.BaseDirectory);
    }

    /// <summary>
    /// Builds the <c>Services:&lt;Name&gt;:*</c> settings of the 5 hosts, with the addresses of the containers.
    /// </summary>
    /// <returns>The settings. A test removes a key to check the host build.</returns>
    public Dictionary<string, string?> CreateSettings()
    {
        var settings = new Dictionary<string, string?>();

        foreach (var service in SeederTestData.ServiceNames)
        {
            AddSettings(settings, service);
        }

        return settings;
    }

    public static IConfiguration CreateConfiguration(IDictionary<string, string?> settings) =>
        new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

    /// <summary>
    /// Applies the migrations and seeds the required data of the 5 services. The first call creates the schemas; the
    /// fixture then keeps a resetter for each database.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>The moment that the clock held for the initialization.</returns>
    public async Task<DateTimeOffset> InitializeHostsAsync(CancellationToken cancellationToken)
    {
        var moment = EnterNextMoment(item: "initialization");
        await Task.WhenAll(_hosts.All.Select(host => host.InitializeAsync(cancellationToken)));
        await CreateResettersAsync();

        return moment;
    }

    /// <summary>
    /// Keeps a resetter for each database. Call it after the schemas exist, so the reset deletes the rows of a test
    /// that initialized the hosts itself.
    /// </summary>
    /// <returns>A task that completes when the resetters exist.</returns>
    public async Task CreateResettersAsync()
    {
        _resetters ??=
        [
            await CreateResetterAsync<CoreDbContext>(_hosts.Core, service: "core"),
            await CreateResetterAsync<TelemetryDbContext>(_hosts.Telemetry, service: "telemetry"),
            await CreateResetterAsync<DocumentDbContext>(_hosts.Document, service: "document"),
            await CreateResetterAsync<InsightsDbContext>(_hosts.Insights, service: "insights"),
            await CreateResetterAsync<AuthDbContext>(_hosts.Auth, service: "auth"),
        ];
    }

    /// <summary>Deletes every row of the 5 databases and clears Redis, so the next test starts on empty tables.
    /// </summary>
    public async ValueTask ResetAsync()
    {
        if (_resetters is not null)
        {
            await Task.WhenAll(_resetters.Select(resetter => resetter.ResetAsync()));
        }

        await _mailpit.DeleteMessagesAsync();
        await _keycloak.DeleteTestIdentitiesAsync();
        await DeleteTrackedOrganizationsAsync();
        Clock.Reset();
        _momentIndex = 0;
        await _redis.FlushAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _hosts.DisposeAsync();
        await _database.DisposeAsync();
        await _storage.DisposeAsync();
        await _mailpit.DisposeAsync();
        await _redis.DisposeAsync();
        await _keycloak.DisposeAsync();
    }

    private async Task<DatabaseResetter> CreateResetterAsync<TContext>(ServiceHost host, string service)
        where TContext : DbContext
    {
        using var scope = host.CreateScope(SeederTestData.InitializationActor);
        var context = scope.ServiceProvider.GetRequiredService<TContext>();

        var connectionString = new NpgsqlConnectionStringBuilder(_database.ConnectionString)
        {
            Database = DatabaseName(service),
            Pooling = false,
        }.ConnectionString;

        return await DatabaseResetter.CreateAsync(connectionString, context);
    }

    public KeycloakTestServer Keycloak => _keycloak;

    public MailpitTestServer Mailpit => _mailpit;

    /// <summary>Builds the connection string of the database of a service.</summary>
    /// <param name="service">The service name (<c>core</c>, <c>telemetry</c>, ...).</param>
    /// <returns>The connection string.</returns>
    public string ConnectionStringOf(string service) =>
        new NpgsqlConnectionStringBuilder(_database.ConnectionString) { Database = DatabaseName(service) }
            .ConnectionString;

    /// <summary>Remembers a company whose Keycloak organization the reset deletes.</summary>
    /// <param name="companyId">The company ID.</param>
    public void TrackOrganization(Guid companyId) => _organizations.Add(companyId);

    private async Task DeleteTrackedOrganizationsAsync()
    {
        var accounts = new KeycloakAccounts(_hosts.Auth);

        foreach (var companyId in _organizations)
        {
            await accounts.DeleteOrganizationWithMembersAsync(companyId, CancellationToken.None);
        }

        _organizations.Clear();
    }

    private static string DatabaseName(string service) => $"seeder_{service}";

    private static void AddIdEncryption(Action<string, string?> set)
    {
        set(arg1: "IdEncryption:Keys:0:Key", SeederTestData.IdEncryptionKey);
    }

    private void AddSettings(Dictionary<string, string?> settings, string service)
    {
        void Set(string key, string? value)
        {
            settings[$"Services:{service}:{key}"] = value;
        }

        var database = new NpgsqlConnectionStringBuilder(_database.ConnectionString)
        {
            Database = DatabaseName(service),
        };

        Set(key: "ConnectionStrings:Database", database.ConnectionString);
        Set(key: "ConnectionStrings:RabbitMq", SeederTestData.RabbitMqUnused);
        Set(key: "Redis:ConnectionString", _redis.ConnectionString);

        switch (service)
        {
            case "core":
                Set(key: "InviteTokens:LifetimeHours", value: "72");
                Set(key: "InviteTokens:Keys:0:Secret", SeederTestData.InviteTokenSecret);
                Set(key: "Smtp:Host", _mailpit.SmtpHost);
                Set(key: "Smtp:Port", _mailpit.SmtpPort.ToString());

                break;
            case "document":
                AddIdEncryption(Set);
                Set(key: "FileStorage:ConnectionString", _storage.ConnectionString);

                break;
            case "insights":
                AddIdEncryption(Set);
                Set(key: "ServiceTokens:TokenEndpoint", $"{SeederTestData.UnusedUrl}/token");
                Set(key: "ServiceTokens:ClientId", value: "healthyfac-insights");
                Set(key: "ServiceTokens:ClientSecret", SeederTestData.UnusedSecret);
                Set(key: "DocumentClient:GrpcAddress", SeederTestData.UnusedUrl);
                Set(key: "TelemetryClient:GrpcAddress", SeederTestData.UnusedUrl);
                Set(key: "CoreClient:GrpcAddress", SeederTestData.UnusedUrl);
                Set(key: "Docling:BaseUrl", SeederTestData.UnusedUrl);
                Set(key: "Docling:ApiKey", SeederTestData.UnusedSecret);
                Set(key: "Gemini:ProjectId", value: "test-project");
                Set(key: "Gemini:Location", value: "test-location");
                Set(key: "Gemini:ChatModel", value: "test-chat-model");
                Set(key: "Gemini:AssistantModel", value: "test-assistant-model");
                Set(key: "Gemini:TriageModel", value: "test-triage-model");

                break;
            case "auth":
                Set(key: "KeycloakAdmin:BaseAddress", _keycloak.BaseAddress.TrimEnd(trimChar: '/'));
                Set(key: "KeycloakAdmin:Realm", SeederTestData.KeycloakRealm);
                Set(key: "KeycloakAdmin:SpaClientId", value: "healthyfac-spa");
                Set(key: "KeycloakAdmin:SpaBaseUrl", SeederTestData.KeycloakSpaUrl);

                Set(
                    key: "ServiceTokens:TokenEndpoint",
                    string.Format(
                        SeederTestData.TokenPathFormat, _keycloak.BaseAddress.TrimEnd(trimChar: '/'),
                        SeederTestData.KeycloakRealm));

                Set(key: "ServiceTokens:ClientId", value: "healthyfac-auth");
                Set(key: "ServiceTokens:ClientSecret", SeederTestData.AuthClientSecret);

                break;
        }
    }
}
