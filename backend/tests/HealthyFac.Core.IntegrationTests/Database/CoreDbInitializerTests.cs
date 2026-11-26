using HealthyFac.Core.Domain.Roles;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Core.Infrastructure.Database.Initialization;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Database;

[Collection(CoreEmployeesCollection.Name)]
public sealed class CoreDbInitializerTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task SeedRequiredDataAsync_RunTwice_KeepsOneRolePerCode()
    {
        await SeedAsync(initializer => initializer.SeedRequiredDataAsync());
        await SeedAsync(initializer => initializer.SeedRequiredDataAsync());

        using var scope = _fixture.ScopeFactory.CreateScope();

        var roles = await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
            .Roles
            .AsNoTracking()
            .ToListAsync(TestContext.Current.CancellationToken);

        roles.Select(role => role.Code).Should().BeEquivalentTo(DatabaseTestData.RoleCodes);
        roles.Select(role => role.Id).Should().OnlyHaveUniqueItems().And.NotContain(Guid.Empty);
    }

    [Fact]
    public async Task SeedRequiredDataAsync_WithChangedRoleName_RestoresSeededName()
    {
        using (var changeScope = _fixture.ScopeFactory.CreateScope())
        {
            await changeScope.ServiceProvider.GetRequiredService<CoreDbContext>()
                .Roles
                .Where(role => role.Code == RoleCode.Worker)
                .ExecuteUpdateAsync(
                    update => update.SetProperty(role => role.Name, DatabaseTestData.ChangedRoleName),
                    TestContext.Current.CancellationToken);
        }

        await SeedAsync(initializer => initializer.SeedRequiredDataAsync());

        using var scope = _fixture.ScopeFactory.CreateScope();

        var workerName = await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
            .Roles
            .Where(role => role.Code == RoleCode.Worker)
            .Select(role => role.Name)
            .SingleAsync(TestContext.Current.CancellationToken);

        workerName.Should().NotBe(DatabaseTestData.ChangedRoleName);
    }

    [Fact]
    public async Task SeedRequiredDataAsync_WithSoftDeletedRole_AddsOneActiveRoleAndKeepsDeletedRowDeleted()
    {
        await SeedAsync(initializer => initializer.SeedRequiredDataAsync());

        using (var deleteScope = _fixture.ScopeFactory.CreateScope())
        {
            await deleteScope.ServiceProvider.GetRequiredService<CoreDbContext>()
                .Roles
                .Where(role => role.Code == RoleCode.Supervisor)
                .ExecuteUpdateAsync(
                    update => update.SetProperty(role => role.DeletedAt, DateTime.UtcNow),
                    TestContext.Current.CancellationToken);
        }

        await SeedAsync(initializer => initializer.SeedRequiredDataAsync());

        using var scope = _fixture.ScopeFactory.CreateScope();

        var supervisors = await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
            .Roles
            .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .AsNoTracking()
            .Where(role => role.Code == RoleCode.Supervisor)
            .ToListAsync(TestContext.Current.CancellationToken);

        supervisors.Where(role => role.DeletedAt is null).Should().ContainSingle();
        supervisors.Where(role => role.DeletedAt is not null).Should().ContainSingle();
    }

    private async Task SeedAsync(Func<ICoreDbInitializer, Task> seed)
    {
        using var scope = _fixture.ScopeFactory.CreateScope();

        await seed(scope.ServiceProvider.GetRequiredService<ICoreDbInitializer>());
    }
}
