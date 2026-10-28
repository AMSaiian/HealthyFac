using HealthyFac.Core.Domain.Companies;
using HealthyFac.Core.Domain.Departments;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Testing.Authentication;
using HealthyFac.Testing.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.ApiTests.Hosting;

/// <summary>
/// Starts PostgreSQL, RabbitMQ, and the Core host once for all tests of a class. The fixture migrates the schema
/// before the host starts, because the host migrates only in the Development environment. It then seeds one company
/// with one department, which the tokens of the tests name in their <c>organization</c> claim.
/// </summary>
public sealed class CoreApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlTestDatabase _database = new();
    private readonly RabbitMqTestBroker _broker = new();

    private CoreApiFactory _factory = null!;

    public TestTokenFactory Tokens { get; } = new();

    public Guid CompanyId { get; private set; }

    public Guid DepartmentId { get; private set; }

    public IServiceScopeFactory ScopeFactory => _factory.Services.GetRequiredService<IServiceScopeFactory>();

    public IIdEncryptor IdEncryptor => _factory.Services.GetRequiredService<IIdEncryptor>();

    public HttpClient CreateClient() => _factory.CreateClient();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_database.StartAsync(), _broker.StartAsync());

        await MigrateAsync();

        _factory = new CoreApiFactory(
            CoreApiTestData.CreateSettings(_database.ConnectionString, _broker.ConnectionString),
            Tokens);

        await SeedCompanyAsync();
    }

    public async Task DisposeAsync()
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
        using var scope = ScopeFactory.CreateActorScope(SystemActor.Create());
        var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        var company = new Company { Name = CoreApiTestData.CompanyName };
        var department = new Department
        {
            CompanyId = company.Id,
            Name = CoreApiTestData.DepartmentName,
            TimeZoneId = CoreApiTestData.DepartmentTimeZoneId,
        };
        dbContext.Companies.Add(company);
        dbContext.Departments.Add(department);
        await dbContext.SaveChangesAsync();

        CompanyId = company.Id;
        DepartmentId = department.Id;
    }
}
