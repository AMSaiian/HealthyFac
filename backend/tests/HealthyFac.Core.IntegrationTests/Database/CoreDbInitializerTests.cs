using HealthyFac.Core.Domain.Roles;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Core.Infrastructure.Database.Seeding;
using HealthyFac.Core.IntegrationTests.Database.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HealthyFac.Core.IntegrationTests.Database;

public sealed class CoreDbInitializerTests(CoreDatabaseFixture fixture) : IClassFixture<CoreDatabaseFixture>
{
    [Fact]
    public async Task SeedRequiredDataAsync_RunTwice_KeepsOneRolePerCode()
    {
        await SeedAsync(initializer => initializer.SeedRequiredDataAsync());
        await SeedAsync(initializer => initializer.SeedRequiredDataAsync());

        using var scope = fixture.ScopeFactory.CreateScope();
        var roles = await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
            .Roles
            .AsNoTracking()
            .ToListAsync();

        roles.Select(role => role.Code).Should().BeEquivalentTo(DatabaseTestData.RoleCodes);
        roles.Select(role => role.Id).Should().OnlyHaveUniqueItems().And.NotContain(Guid.Empty);
    }

    [Fact]
    public async Task SeedRequiredDataAsync_WithChangedRoleName_RestoresSeededName()
    {
        using (var changeScope = fixture.ScopeFactory.CreateScope())
        {
            await changeScope.ServiceProvider.GetRequiredService<CoreDbContext>()
                .Roles
                .Where(role => role.Code == RoleCode.Worker)
                .ExecuteUpdateAsync(update => update.SetProperty(role => role.Name, DatabaseTestData.ChangedRoleName));
        }

        await SeedAsync(initializer => initializer.SeedRequiredDataAsync());

        using var scope = fixture.ScopeFactory.CreateScope();
        var workerName = await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
            .Roles
            .Where(role => role.Code == RoleCode.Worker)
            .Select(role => role.Name)
            .SingleAsync();
        workerName.Should().NotBe(DatabaseTestData.ChangedRoleName);
    }

    [Fact]
    public async Task SeedOptionalDataAsync_RunTwice_LeavesFirstRunEntitiesUnchanged()
    {
        await SeedAsync(initializer => initializer.SeedOptionalDataAsync());
        var firstRun = await ReadSnapshotAsync();

        await SeedAsync(initializer => initializer.SeedOptionalDataAsync());

        var secondRun = await ReadSnapshotAsync();
        firstRun.Companies.Should().NotBeEmpty();
        firstRun.Employees.Should().NotBeEmpty().And.OnlyContain(employee => employee.Departments.Count > 0);
        secondRun.Should().BeEquivalentTo(firstRun, options => options.IgnoringCyclicReferences());
    }

    [Fact]
    public async Task SeedOptionalDataAsync_OnEmptyDatabase_NamesMachinesAfterConfiguredTypes()
    {
        await SeedAsync(initializer => initializer.SeedOptionalDataAsync());

        using var scope = fixture.ScopeFactory.CreateScope();
        var machineTypes = scope.ServiceProvider.GetRequiredService<IOptions<SeedingOptions>>()
            .Value
            .DevelopmentData
            .GetMachineTypes();
        var machineNames = await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
            .Machines
            .Select(machine => machine.Name)
            .ToListAsync();

        machineNames.Should()
            .NotBeEmpty()
            .And.OnlyContain(name => machineTypes.Any(type => name.StartsWith(type + " ")));
    }

    [Fact]
    public async Task SeedAsync_WithForcedRecreation_ReplacesAllBusinessData()
    {
        await SeedAsync(initializer => initializer.SeedOptionalDataAsync());
        var companyIdsBefore = await ReadCompanyIdsAsync();
        using var recreateScope = fixture.ScopeFactory.CreateScope();
        var seeder = new CoreDevelopmentDataSeeder(
            recreateScope.ServiceProvider.GetRequiredService<CoreDbContext>(),
            recreateScope.ServiceProvider.GetRequiredService<TimeProvider>(),
            Options.Create(DatabaseTestData.ForcedRecreationOptions),
            NullLogger<CoreDevelopmentDataSeeder>.Instance);

        await seeder.SeedAsync(CancellationToken.None);

        var companyIdsAfter = await ReadCompanyIdsAsync();
        using var scope = fixture.ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var departmentCount = await dbContext.Departments.CountAsync();
        var machineCount = await dbContext.Machines.CountAsync();
        var inviteCount = await dbContext.EmployeeInvites.CountAsync();
        var employeeCount = await dbContext.Employees.CountAsync();

        companyIdsAfter.Should()
            .HaveCount(DatabaseTestData.SeededCompanyCount)
            .And.NotIntersectWith(companyIdsBefore);
        departmentCount.Should().Be(DatabaseTestData.SeededDepartmentCount);
        machineCount.Should().Be(DatabaseTestData.SeededMachineCount);
        inviteCount.Should().Be(DatabaseTestData.SeededInviteCount);
        employeeCount.Should().Be(DatabaseTestData.SeededEmployeeCount);
    }

    private async Task<SeededDataSnapshot> ReadSnapshotAsync()
    {
        using var scope = fixture.ScopeFactory.CreateScope();

        return await SeededDataSnapshot.ReadAsync(scope.ServiceProvider.GetRequiredService<CoreDbContext>());
    }

    private async Task<List<Guid>> ReadCompanyIdsAsync()
    {
        using var scope = fixture.ScopeFactory.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
            .Companies
            .Select(company => company.Id)
            .ToListAsync();
    }

    private async Task SeedAsync(Func<ICoreDbInitializer, Task> seed)
    {
        using var scope = fixture.ScopeFactory.CreateScope();

        await seed(scope.ServiceProvider.GetRequiredService<ICoreDbInitializer>());
    }
}
