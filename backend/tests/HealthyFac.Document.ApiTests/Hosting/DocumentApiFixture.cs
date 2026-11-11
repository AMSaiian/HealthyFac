using HealthyFac.Document.Infrastructure.Database;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Testing.Authentication;
using HealthyFac.Testing.Containers;
using HealthyFac.Testing.Persistence;
using HealthyFac.Testing.Resetting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Document.ApiTests.Hosting;

/// <summary>
/// Starts PostgreSQL, RabbitMQ, and the Document host once for all tests of the <c>DocumentApi</c> collection. The
/// fixture migrates the schema before the host starts, because the host migrates only in the Development environment.
/// The fixture only names a company, which the tokens of the tests carry and the registrations of
/// the tests use. <see cref="ResetAsync"/> runs before each test and deletes the rows that tests create.
/// </summary>
public sealed class DocumentApiFixture : IAsyncLifetime, IResetBeforeEachTest
{
    private readonly PostgreSqlTestDatabase _database = new();
    private readonly RabbitMqTestBroker _broker = new();
    private readonly AzuriteTestStorage _storage = new();

    private DocumentApiFactory _factory = null!;
    private DatabaseResetter _resetter = null!;

    public TestTokenFactory Tokens { get; } = new();

    public string StorageConnectionString => _storage.ConnectionString;

    public Guid CompanyId { get; } = Guid.CreateVersion7();

    public IServiceScopeFactory ScopeFactory => _factory.Services.GetRequiredService<IServiceScopeFactory>();

    public IIdEncryptor IdEncryptor => _factory.Services.GetRequiredService<IIdEncryptor>();

    public HttpClient CreateClient() => _factory.CreateClient();

    /// <summary>
    /// Starts a second host with the storage endpoint of a connection string, for a test of a storage outage.
    /// </summary>
    /// <param name="storageConnection">The storage connection string that the host uses.</param>
    /// <param name="configureServices">Replaces services of the host, or <see langword="null"/>.</param>
    /// <returns>The host. The caller disposes it.</returns>
    public DocumentApiFactory CreateFactoryWithStorage(
        string storageConnection,
        Action<IServiceCollection>? configureServices = null
    ) =>
        new(
            DocumentApiTestData.CreateSettings(
                _database.ConnectionString, _broker.ConnectionString, storageConnection),
            Tokens,
            configureServices);

    public TestTokenFactory TokenFactory => Tokens;

    /// <summary>
    /// Starts a second host on real Kestrel ports for the gRPC tests. It shares the containers of the fixture.
    /// </summary>
    /// <returns>The host. The caller disposes it.</returns>
    public DocumentGrpcHost CreateGrpcHost() =>
        new(
            DocumentApiTestData.CreateSettings(
                _database.ConnectionString, _broker.ConnectionString, _storage.ConnectionString),
            Tokens);

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_database.StartAsync(), _broker.StartAsync(), _storage.StartAsync());

        await MigrateAsync();

        _factory = new DocumentApiFactory(
            DocumentApiTestData.CreateSettings(
                _database.ConnectionString, _broker.ConnectionString, _storage.ConnectionString),
            Tokens);

        _ = _factory.Services;

        await using var dbContext = new DocumentDbContext(
            new DbContextOptionsBuilder<DocumentDbContext>().UseNpgsql(_database.ConnectionString).Options);
        _resetter = await DatabaseResetter.CreateAsync(_database.ConnectionString, dbContext);
    }

    public async ValueTask ResetAsync() => await _resetter.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _broker.DisposeAsync();
        await _database.DisposeAsync();
        await _storage.DisposeAsync();
    }

    private async Task MigrateAsync()
    {
        var options = new DbContextOptionsBuilder<DocumentDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .Options;

        await using var dbContext = new DocumentDbContext(options);
        await dbContext.Database.MigrateAsync();
    }
}
