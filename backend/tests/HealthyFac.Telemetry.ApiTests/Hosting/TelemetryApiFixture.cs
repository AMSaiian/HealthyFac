using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Telemetry.Infrastructure.Database;
using HealthyFac.Testing.Authentication;
using HealthyFac.Testing.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Telemetry.ApiTests.Hosting;

/// <summary>
/// Starts PostgreSQL, RabbitMQ, and the Telemetry host once for all tests of a class. The fixture migrates the schema
/// before the host starts, because the host migrates only in the Development environment. The host seeds the unit
/// catalog at start. Telemetry owns no company data, so the fixture only names a company and a department, which the
/// tokens of the tests carry and the registrations of the tests use.
/// </summary>
public sealed class TelemetryApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlTestDatabase _database = new();
    private readonly RabbitMqTestBroker _broker = new();

    private TelemetryApiFactory _factory = null!;

    public TestTokenFactory Tokens { get; } = new();

    public Guid CompanyId { get; } = Guid.CreateVersion7();

    public Guid DepartmentId { get; } = Guid.CreateVersion7();

    public IServiceScopeFactory ScopeFactory => _factory.Services.GetRequiredService<IServiceScopeFactory>();

    public IIdEncryptor IdEncryptor => _factory.Services.GetRequiredService<IIdEncryptor>();

    public HttpClient CreateClient() => _factory.CreateClient();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_database.StartAsync(), _broker.StartAsync());

        await MigrateAsync();

        _factory = new TelemetryApiFactory(
            TelemetryApiTestData.CreateSettings(_database.ConnectionString, _broker.ConnectionString),
            Tokens);

        _ = _factory.Services;
    }

    public async Task DisposeAsync()
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
