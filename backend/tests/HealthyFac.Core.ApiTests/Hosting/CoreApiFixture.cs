using HealthyFac.Core.Domain.Companies;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Testing.Authentication;
using HealthyFac.Testing.Containers;
using HealthyFac.Testing.Persistence;
using HealthyFac.Testing.Resetting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.ApiTests.Hosting;

/// <summary>
/// Starts PostgreSQL, RabbitMQ, and the Core host once for all tests of the <c>CoreApi</c> collection. The fixture
/// migrates the schema before the host starts, because the host migrates only in the Development environment. It then
/// seeds one company, which the tokens of the tests name in their <c>organization</c> claim.
/// <see cref="ResetAsync"/> runs before each test and deletes the rows that tests create.
/// </summary>
public sealed class CoreApiFixture : IAsyncLifetime, IResetBeforeEachTest
{
    private readonly PostgreSqlTestDatabase _database = new();
    private readonly RabbitMqTestBroker _broker = new();

    private CoreApiFactory _factory = null!;
    private DatabaseResetter _resetter = null!;

    public TestTokenFactory Tokens { get; } = new();

    public Guid CompanyId { get; private set; }

    public IServiceScopeFactory ScopeFactory => _factory.Services.GetRequiredService<IServiceScopeFactory>();

    public IIdEncryptor IdEncryptor => _factory.Services.GetRequiredService<IIdEncryptor>();

    public HttpClient CreateClient() => _factory.CreateClient();

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_database.StartAsync(), _broker.StartAsync());

        await MigrateAsync();

        _factory = new CoreApiFactory(
            CoreApiTestData.CreateSettings(_database.ConnectionString, _broker.ConnectionString),
            Tokens);

        await SeedCompanyAsync();

        using var scope = ScopeFactory.CreateScope();
        _resetter = await DatabaseResetter.CreateAsync(
            _database.ConnectionString,
            scope.ServiceProvider.GetRequiredService<CoreDbContext>(),
            typeof(Company),
            typeof(Role));
    }

    /// <summary>
    /// Deletes the rows that tests create. The company and the roles stay, because the tokens of the
    /// tests name their IDs and no test writes them.
    /// </summary>
    public async ValueTask ResetAsync() => await _resetter.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _broker.DisposeAsync();
        await _database.DisposeAsync();
    }

    private async Task MigrateAsync()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .Options;

        await using var dbContext = new CoreDbContext(options);
        await dbContext.Database.MigrateAsync();
    }

    private async Task SeedCompanyAsync()
    {
        using var scope = ScopeFactory.CreateActorScope(ServiceActor.DatabaseInitialization.For(ServiceName.Core));
        var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        var company = new Company
            { Name = CoreApiTestData.CompanyName, TimeZoneId = CoreApiTestData.CompanyTimeZoneId };
        dbContext.Companies.Add(company);
        await dbContext.SaveChangesAsync();

        CompanyId = company.Id;
    }
}
