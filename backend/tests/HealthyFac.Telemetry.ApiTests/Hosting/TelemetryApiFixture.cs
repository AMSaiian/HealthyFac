using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Telemetry.Infrastructure.Database;
using HealthyFac.Telemetry.Infrastructure.Database.Initialization;
using HealthyFac.Testing.Authentication;
using HealthyFac.Testing.Containers;
using HealthyFac.Testing.Persistence;
using HealthyFac.Testing.Resetting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Telemetry.ApiTests.Hosting;

/// <summary>
/// Starts PostgreSQL, RabbitMQ, and the Telemetry host once for all tests of the <c>TelemetryApi</c> collection. The
/// fixture migrates the schema before the host starts, because the host migrates only in the Development environment.
/// The host seeds the unit catalog at start. Telemetry owns no company data, so the fixture only names a company, which
/// the tokens of the tests carry and the registrations of the tests use. <see cref="ResetAsync"/>
/// runs before each test, deletes the rows that tests create, and seeds the unit catalog again.
/// </summary>
public sealed class TelemetryApiFixture : IAsyncLifetime, IResetBeforeEachTest
{
    private readonly PostgreSqlTestDatabase _database = new();
    private readonly RabbitMqTestBroker _broker = new();

    private TelemetryApiFactory _factory = null!;
    private DatabaseResetter _resetter = null!;

    public TestTokenFactory Tokens { get; } = new();

    public Guid CompanyId { get; } = Guid.CreateVersion7();

    public IServiceScopeFactory ScopeFactory => _factory.Services.GetRequiredService<IServiceScopeFactory>();

    public IIdEncryptor IdEncryptor => _factory.Services.GetRequiredService<IIdEncryptor>();

    public HttpClient CreateClient() => _factory.CreateClient();

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_database.StartAsync(), _broker.StartAsync());

        await MigrateAsync();

        _factory = new TelemetryApiFactory(
            TelemetryApiTestData.CreateSettings(_database.ConnectionString, _broker.ConnectionString),
            Tokens);

        _ = _factory.Services;

        await using var dbContext = new TelemetryDbContext(
            new DbContextOptionsBuilder<TelemetryDbContext>().UseNpgsql(_database.ConnectionString).Options);
        _resetter = await DatabaseResetter.CreateAsync(_database.ConnectionString, dbContext);
    }

    /// <summary>
    /// Deletes every row and seeds the unit catalog again, which the host seeded at start.
    /// </summary>
    public async ValueTask ResetAsync()
    {
        await _resetter.ResetAsync();

        using var scope = ScopeFactory.CreateActorScope(ServiceActor.DatabaseInitialization.For(ServiceName.Telemetry));
        await scope.ServiceProvider.GetRequiredService<ITelemetryDbInitializer>().SeedRequiredDataAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _broker.DisposeAsync();
        await _database.DisposeAsync();
    }

    private async Task MigrateAsync()
    {
        var options = new DbContextOptionsBuilder<TelemetryDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .Options;

        await using var dbContext = new TelemetryDbContext(options);
        await dbContext.Database.MigrateAsync();
    }
}
