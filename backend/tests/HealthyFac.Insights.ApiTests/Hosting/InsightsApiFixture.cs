using HealthyFac.Insights.Application.Files;
using HealthyFac.Insights.Infrastructure.Database;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Testing.Authentication;
using HealthyFac.Testing.Containers;
using HealthyFac.Testing.Persistence;
using HealthyFac.Testing.Resetting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HealthyFac.Insights.ApiTests.Hosting;

/// <summary>
/// Starts PostgreSQL, RabbitMQ, Redis, and the Insights host once for all tests of the <c>InsightsApi</c> collection.
/// The fixture creates the schema before the host starts, because the host migrates only in the Development
/// environment. The fixture only names a company, which the tokens of the tests carry.
/// <see cref="ResetAsync"/> runs before each test and deletes the rows that tests create.
/// </summary>
public sealed class InsightsApiFixture : IAsyncLifetime, IResetBeforeEachTest
{
    private readonly PostgreSqlTestDatabase _database = new();
    private readonly RabbitMqTestBroker _broker = new();
    private readonly RedisTestServer _redis = new();

    private InsightsApiFactory _factory = null!;
    private DatabaseResetter _resetter = null!;

    public TestTokenFactory Tokens { get; } = new();

    public FakeFileCatalog Files { get; } = new();

    public IIdEncryptor IdEncryptor => _factory.Services.GetRequiredService<IIdEncryptor>();

    public Guid CompanyId { get; } = Guid.CreateVersion7();

    public IServiceScopeFactory ScopeFactory => _factory.Services.GetRequiredService<IServiceScopeFactory>();

    public HttpClient CreateClient() => _factory.CreateClient();

    /// <summary>
    /// Starts another host on the containers of the fixture, with its own service replacements. The caller disposes it.
    /// </summary>
    /// <param name="configureServices">Replaces services of the host.</param>
    /// <returns>The host.</returns>
    public InsightsApiFactory CreateFactory(Action<IServiceCollection> configureServices) =>
        new(
            InsightsApiTestData.CreateSettings(
                _database.ConnectionString, _broker.ConnectionString, _redis.ConnectionString),
            Tokens,
            configureServices);

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_database.StartAsync(), _broker.StartAsync(), _redis.StartAsync());

        var options = new DbContextOptionsBuilder<InsightsDbContext>()
            .UseNpgsql(_database.ConnectionString, npgsql => npgsql.UseVector())
            .Options;
        await using var dbContext = new InsightsDbContext(options);
        await dbContext.Database.MigrateAsync();

        _factory = new InsightsApiFactory(
            InsightsApiTestData.CreateSettings(
                _database.ConnectionString, _broker.ConnectionString, _redis.ConnectionString),
            Tokens,
            services =>
            {
                services.RemoveAll<IFileCatalog>();
                services.AddSingleton<IFileCatalog>(Files);
            });

        _ = _factory.Services;

        _resetter = await DatabaseResetter.CreateAsync(_database.ConnectionString, dbContext);
    }

    public async ValueTask ResetAsync()
    {
        Files.Clear();
        await _resetter.ResetAsync();
        await _redis.FlushAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _broker.DisposeAsync();
        await _database.DisposeAsync();
        await _redis.DisposeAsync();
    }
}
