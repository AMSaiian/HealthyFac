using HealthyFac.Core.Application.Employees.AcceptEmployeeInvite;
using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;

namespace HealthyFac.Core.IntegrationTests.Database;

public sealed class TestTenantSeedingTests(CoreDatabaseFixture fixture) : IClassFixture<CoreDatabaseFixture>
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task SeedOptionalData_WithTestTenant_CreatesCompanyAndActiveSupervisorWithFixedIds()
    {
        await _fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithTestTenantOptions);

        var supervisor = await _fixture.LoadSeededEmployeeAsync(DatabaseTestData.TestSupervisorEmployeeId);
        var companyCount = await _fixture.CountCompaniesAsync(DatabaseTestData.TestTenantCompanyId);
        var departmentCount = await _fixture.CountDepartmentsAsync(DatabaseTestData.TestTenantCompanyId);

        companyCount.Should().Be(expected: 1);
        departmentCount.Should().Be(DatabaseTestData.TestTenantDepartmentCount);
        supervisor.CompanyId.Should().Be(DatabaseTestData.TestTenantCompanyId);
        supervisor.IdentityUserId.Should().Be(DatabaseTestData.TestSupervisorIdentityId);
        supervisor.Email.Should().Be(DatabaseTestData.TestSupervisorEmail);
        supervisor.Status.Should().Be(EmployeeStatus.Active);
        supervisor.RoleCode.Should().Be(RoleCode.Supervisor);
        supervisor.DepartmentCount.Should().Be(DatabaseTestData.TestTenantDepartmentCount);
    }

    [Fact]
    public async Task SeedTestTenant_Always_UsesConfiguredDepartmentIds()
    {
        await _fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithTestTenantOptions);

        var departmentIds = await _fixture.LoadDepartmentIdsAsync(DatabaseTestData.TestTenantCompanyId);

        departmentIds.Should().BeEquivalentTo(DatabaseTestData.TestTenant.DepartmentIds);
    }

    [Fact]
    public async Task TestTenantSeeding_Always_HasMachinesInEveryState()
    {
        await _fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithTestTenantOptions);

        var states = await _fixture.LoadTenantCurrentStatesAsync(DatabaseTestData.TestTenantCompanyId);

        states.Should().Contain([MachineState.Operational, MachineState.InMaintenance, MachineState.OutOfService]);
    }

    [Fact]
    public async Task SeedOptionalData_WithTestTenant_AddsNoEmployeeAcceptedOutboxRow()
    {
        await _fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithTestTenantOptions);

        var outboxCount = await _fixture.CountOutboxMessagesAsync(EmployeeAccepted.MessageType);

        outboxCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task SeedOptionalDataAsync_WithSeedMachines_CreatesMachinesWithConfiguredIds()
    {
        await _fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithSeedMachinesOptions);

        var first = await _fixture.LoadMachineRowAsync(DatabaseTestData.FirstSeedMachineId);
        var second = await _fixture.LoadMachineRowAsync(DatabaseTestData.SecondSeedMachineId);

        first.GatewayId.Should().Be(DatabaseTestData.FirstSeedGatewayId);
        first.DepartmentId.Should().Be(DatabaseTestData.FirstTestDepartmentId);
        first.CompanyId.Should().Be(DatabaseTestData.TestTenantCompanyId);
        second.GatewayId.Should().Be(DatabaseTestData.SecondSeedGatewayId);
        second.DepartmentId.Should().Be(DatabaseTestData.SecondTestDepartmentId);
    }

    [Fact]
    public async Task SeedOptionalDataAsync_WithSeedMachines_StoresMachineCreatedOutboxRowPerMachine()
    {
        await _fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithSeedMachinesOptions);

        var messages = await _fixture.ReadOutboxMessagesAsync<MachineCreated>();

        messages.Should()
            .HaveCount(
                DatabaseTestData.SeededMachineCount
                + DatabaseTestData.TestTenantDepartmentCount * DatabaseTestData.MachinesPerDepartment
                + DatabaseTestData.SeedMachines.Count);
        messages.Should()
            .Contain(message => message.MachineId == DatabaseTestData.FirstSeedMachineId
                && message.GatewayId == DatabaseTestData.FirstSeedGatewayId
                && message.DepartmentId == DatabaseTestData.FirstTestDepartmentId);
        messages.Select(message => message.MachineId).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task SeedOptionalData_TwiceWithTestTenant_CreatesOneCompany()
    {
        await _fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithTestTenantOptions);
        var companyCountAfterFirstRun = await _fixture.CountCompaniesAsync();

        await _fixture.RunSeederAsync(DatabaseTestData.TestTenantOptions);

        var tenantCompanyCount = await _fixture.CountCompaniesAsync(DatabaseTestData.TestTenantCompanyId);
        var companyCount = await _fixture.CountCompaniesAsync();
        tenantCompanyCount.Should().Be(expected: 1);
        companyCount.Should().Be(companyCountAfterFirstRun);
    }

    [Fact]
    public async Task SeedOptionalData_WithTestTenant_CreatesMachinesInEachDepartment()
    {
        await _fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithTestTenantOptions);

        var machines = await _fixture.LoadTenantMachineAsync(DatabaseTestData.TestTenantCompanyId);

        machines.Should()
            .HaveCount(DatabaseTestData.TestTenantDepartmentCount * DatabaseTestData.MachinesPerDepartment);
        machines.GroupBy(machine => machine.DepartmentId)
            .Select(group => group.Key)
            .Should()
            .BeEquivalentTo(DatabaseTestData.TestTenant.DepartmentIds);
    }

    [Fact]
    public async Task SeedOptionalData_TwiceWithTestTenant_AddsNoMachines()
    {
        await _fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithTestTenantOptions);
        var machinesAfterFirstRun = await _fixture.LoadTenantMachineAsync(DatabaseTestData.TestTenantCompanyId);

        await _fixture.RunSeederAsync(DatabaseTestData.TestTenantOptions);

        var machinesAfterSecondRun = await _fixture.LoadTenantMachineAsync(DatabaseTestData.TestTenantCompanyId);
        machinesAfterSecondRun.Should().BeEquivalentTo(machinesAfterFirstRun);
    }
}
