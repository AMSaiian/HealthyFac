using HealthyFac.Core.Domain.Roles;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Core.Infrastructure.Database.Initialization;
using HealthyFac.Core.Infrastructure.Database.Seeding;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Domain.Emails;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HealthyFac.Core.IntegrationTests.Database;

[Collection(CoreSeedingCollection.Name)]
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

    [Fact]
    public async Task SeedOptionalDataAsync_RunTwice_LeavesFirstRunEntitiesUnchanged()
    {
        await SeedAsync(initializer => initializer.SeedOptionalDataAsync());
        var firstRun = await ReadSnapshotAsync();

        await SeedAsync(initializer => initializer.SeedOptionalDataAsync());

        var secondRun = await ReadSnapshotAsync();
        firstRun.Companies.Should().NotBeEmpty();
        firstRun.Employees.Should().NotBeEmpty();
        secondRun.Should().BeEquivalentTo(firstRun, options => options.IgnoringCyclicReferences());
    }

    [Fact]
    public async Task SeedOptionalDataAsync_OnEmptyDatabase_NamesMachinesAfterConfiguredTypes()
    {
        await SeedAsync(initializer => initializer.SeedOptionalDataAsync());

        using var scope = _fixture.ScopeFactory.CreateScope();
        var machineTypes = scope.ServiceProvider.GetRequiredService<IOptions<SeedingOptions>>()
            .Value
            .DevelopmentData
            .GetMachineTypes();
        var machineNames = await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
            .Machines
            .Select(machine => machine.Name)
            .ToListAsync(TestContext.Current.CancellationToken);

        machineNames.Should()
            .NotBeEmpty()
            .And.OnlyContain(name => machineTypes.Any(type => name.StartsWith(type + " ")));
    }

    [Fact]
    public async Task SeedOptionalDataAsync_OnEmptyDatabase_StartsEveryStateHistoryOperational()
    {
        await SeedAsync(initializer => initializer.SeedOptionalDataAsync());

        var afterSeed = TimeProvider.System.GetUtcNow().UtcDateTime;
        var rows = await _fixture.LoadSeededStateRowsAsync();
        var machines = rows.GroupBy(row => row.MachineId).ToList();

        machines.Should().HaveCount(DatabaseTestData.SeededMachineCount);
        foreach (var machine in machines)
        {
            var timeline = machine.OrderBy(row => row.StartedAt).ToList();
            var machineName = timeline[index: 0].MachineName;
            var cycleIndex = ReadMachineIndex(machineName) % DatabaseTestData.ExpectedHistoriesByCycleIndex.Count;

            timeline.Select(row => row.State)
                .Should()
                .Equal(DatabaseTestData.ExpectedHistoriesByCycleIndex[cycleIndex], $"machine '{machineName}'");
            timeline.Select(row => row.StartedAt).Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
            timeline.Should().OnlyContain(row => row.StartedAt < afterSeed);
        }
    }

    [Fact]
    public async Task SeedOptionalDataAsync_OnEmptyDatabase_StoresStateRowsForEveryCompany()
    {
        await SeedAsync(initializer => initializer.SeedOptionalDataAsync());

        var rows = await _fixture.LoadSeededStateRowsAsync();

        rows.Should().HaveCount(DatabaseTestData.SeededStateHistoryCount);
    }

    [Fact]
    public async Task SeedOptionalDataAsync_OnEmptyDatabase_GivesEveryCompanyAZoneAndItsHeadcount()
    {
        await SeedAsync(initializer => initializer.SeedOptionalDataAsync());

        var companies = await _fixture.LoadSeededCompaniesAsync();

        companies.Should().HaveCount(DatabaseTestData.SeededCompanyCount);
        companies.Select(company => company.TimeZoneId)
            .Should()
            .OnlyContain(zone => DatabaseTestData.SeededTimeZoneIds.Contains(zone));
        companies.Select(company => company.MachineCount)
            .Should()
            .AllBeEquivalentTo(DatabaseTestData.MachinesPerCompany);
        companies.Select(company => company.SupervisorCount)
            .Should()
            .AllBeEquivalentTo(DatabaseTestData.SupervisorsPerCompany);
        companies.Select(company => company.WorkerCount).Should().AllBeEquivalentTo(DatabaseTestData.WorkersPerCompany);
    }

    [Fact]
    public async Task SeedOptionalDataAsync_OnEmptyDatabase_StoresNormalizedEmails()
    {
        await SeedAsync(initializer => initializer.SeedOptionalDataAsync());

        using var scope = _fixture.ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var employeeEmails = await dbContext.Employees.Select(employee => employee.Email)
            .ToListAsync(TestContext.Current.CancellationToken);
        var inviteEmails = await dbContext.EmployeeInvites.Select(invite => invite.Email)
            .ToListAsync(TestContext.Current.CancellationToken);

        employeeEmails.Should().NotBeEmpty().And.OnlyContain(email => email == EmailAddresses.Normalize(email));
        inviteEmails.Should().NotBeEmpty().And.OnlyContain(email => email == EmailAddresses.Normalize(email));
    }

    [Fact]
    public async Task SeedAsync_WithForcedRecreation_ReplacesAllBusinessData()
    {
        await SeedAsync(initializer => initializer.SeedOptionalDataAsync());
        var companyIdsBefore = await ReadCompanyIdsAsync();
        var stateRowIdsBefore = await _fixture.SoftDeleteOneStateRowAndLoadAllIdsAsync();
        using var recreateScope = _fixture.ScopeFactory.CreateScope();
        var seeder = new CoreDevelopmentDataSeeder(
            recreateScope.ServiceProvider.GetRequiredService<CoreDbContext>(),
            recreateScope.ServiceProvider.GetRequiredService<TimeProvider>(),
            Options.Create(DatabaseTestData.ForcedRecreationOptions),
            NullLogger<CoreDevelopmentDataSeeder>.Instance,
            _fixture.InviteTokenService,
            recreateScope.ServiceProvider.GetRequiredService<IOutbox>());

        await seeder.SeedAsync(CancellationToken.None);

        var companyIdsAfter = await ReadCompanyIdsAsync();
        var stateRowIdsAfter = await _fixture.LoadAllStateRowIdsAsync();
        using var scope = _fixture.ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var machineCount = await dbContext.Machines.CountAsync(TestContext.Current.CancellationToken);
        var inviteCount = await dbContext.EmployeeInvites.CountAsync(TestContext.Current.CancellationToken);
        var employeeCount = await dbContext.Employees.CountAsync(TestContext.Current.CancellationToken);

        companyIdsAfter.Should()
            .HaveCount(DatabaseTestData.SeededCompanyCount)
            .And.NotIntersectWith(companyIdsBefore);
        stateRowIdsAfter.Should()
            .HaveCount(DatabaseTestData.SeededStateHistoryCount)
            .And.NotIntersectWith(stateRowIdsBefore);
        machineCount.Should().Be(DatabaseTestData.SeededMachineCount);
        inviteCount.Should().Be(DatabaseTestData.SeededInviteCount);
        employeeCount.Should().Be(DatabaseTestData.SeededEmployeeCount);
    }

    private static int ReadMachineIndex(string machineName) =>
        int.Parse(machineName[^DatabaseTestData.MachineNameNumberLength..]) - 1;

    private async Task<SeededDataSnapshot> ReadSnapshotAsync()
    {
        using var scope = _fixture.ScopeFactory.CreateScope();

        return await SeededDataSnapshot.ReadAsync(scope.ServiceProvider.GetRequiredService<CoreDbContext>());
    }

    private async Task<List<Guid>> ReadCompanyIdsAsync()
    {
        using var scope = _fixture.ScopeFactory.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
            .Companies
            .Select(company => company.Id)
            .ToListAsync();
    }

    private async Task SeedAsync(Func<ICoreDbInitializer, Task> seed)
    {
        using var scope = _fixture.ScopeFactory.CreateScope();

        await seed(scope.ServiceProvider.GetRequiredService<ICoreDbInitializer>());
    }
}
