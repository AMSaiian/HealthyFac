using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Core.Domain.Shifts;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Core.Messaging.Employees;
using HealthyFac.Core.Messaging.Machines;

namespace HealthyFac.Core.IntegrationTests.Database;

[Collection(CoreSeedingCollection.Name)]
public sealed class TestTenantSeedingTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task SeedOptionalData_WithTestTenant_CreatesCompanyAndActiveSupervisorWithFixedIds()
    {
        await _fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithTestTenantOptions);

        var supervisor = await _fixture.LoadSeededEmployeeAsync(DatabaseTestData.TestSupervisorEmployeeId);
        var companyCount = await _fixture.CountCompaniesAsync(DatabaseTestData.TestTenantCompanyId);
        var timeZoneId = await _fixture.LoadCompanyTimeZoneIdAsync(DatabaseTestData.TestTenantCompanyId);

        companyCount.Should().Be(expected: 1);
        timeZoneId.Should().Be(DatabaseTestData.TestTenantTimeZoneId);
        supervisor.CompanyId.Should().Be(DatabaseTestData.TestTenantCompanyId);
        supervisor.IdentityUserId.Should().Be(DatabaseTestData.TestSupervisorIdentityId);
        supervisor.Email.Should().Be(DatabaseTestData.TestSupervisorEmail);
        supervisor.Status.Should().Be(EmployeeStatus.Active);
        supervisor.RoleCode.Should().Be(RoleCode.Supervisor);
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
        first.CompanyId.Should().Be(DatabaseTestData.TestTenantCompanyId);
        second.GatewayId.Should().Be(DatabaseTestData.SecondSeedGatewayId);
    }

    [Fact]
    public async Task SeedOptionalDataAsync_WithASeedMachineName_UsesItAndElseTheNumberedName()
    {
        await _fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithSeedMachinesOptions);

        var first = await _fixture.LoadMachineRowAsync(DatabaseTestData.FirstSeedMachineId);
        var second = await _fixture.LoadMachineRowAsync(DatabaseTestData.SecondSeedMachineId);

        first.Name.Should().Be(DatabaseTestData.NamedSeedMachineName);
        second.Name.Should().Be(expected: "Seed machine 02");
    }

    [Fact]
    public async Task SeedOptionalDataAsync_WithSeedMachines_StoresMachineCreatedOutboxRowPerMachine()
    {
        await _fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithSeedMachinesOptions);

        var messages = await _fixture.ReadOutboxMessagesAsync<MachineCreated>();

        messages.Should()
            .HaveCount(
                DatabaseTestData.SeededMachineCount
                + DatabaseTestData.MachinesPerCompany
                + DatabaseTestData.SeedMachines.Count);
        messages.Should()
            .Contain(message => message.MachineId == DatabaseTestData.FirstSeedMachineId
                && message.GatewayId == DatabaseTestData.FirstSeedGatewayId);
        messages.Select(message => message.MachineId).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task SeedOptionalData_WithTestTenant_CreatesMachinesOfTheCompany()
    {
        await _fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithTestTenantOptions);

        var machines = await _fixture.LoadTenantMachineAsync(DatabaseTestData.TestTenantCompanyId);

        machines.Should().HaveCount(DatabaseTestData.MachinesPerCompany);
    }

    [Fact]
    public async Task SeedOptionalData_TwiceWithTestTenant_AddsNoCompanyAndNoMachines()
    {
        await _fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithTestTenantOptions);
        var companyCountAfterFirstRun = await _fixture.CountCompaniesAsync();
        var machinesAfterFirstRun = await _fixture.LoadTenantMachineAsync(DatabaseTestData.TestTenantCompanyId);

        await _fixture.RunSeederAsync(DatabaseTestData.TestTenantOptions);

        var tenantCompanyCount = await _fixture.CountCompaniesAsync(DatabaseTestData.TestTenantCompanyId);
        var companyCount = await _fixture.CountCompaniesAsync();
        var machinesAfterSecondRun = await _fixture.LoadTenantMachineAsync(DatabaseTestData.TestTenantCompanyId);

        tenantCompanyCount.Should().Be(expected: 1);
        companyCount.Should().Be(companyCountAfterFirstRun);

        machinesAfterSecondRun.Should().BeEquivalentTo(machinesAfterFirstRun);
    }

    [Fact]
    public async Task SeedOptionalData_WithWorker_CreatesActiveWorkerWithFixedIds()
    {
        await _fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithTestTenantOptions);

        var worker = await _fixture.LoadSeededEmployeeAsync(DatabaseTestData.TestWorkerEmployeeId);

        worker.CompanyId.Should().Be(DatabaseTestData.TestTenantCompanyId);
        worker.IdentityUserId.Should().Be(DatabaseTestData.TestWorkerIdentityId);
        worker.Email.Should().Be(DatabaseTestData.TestWorkerEmail);
        worker.Status.Should().Be(EmployeeStatus.Active);
        worker.RoleCode.Should().Be(RoleCode.Worker);
    }

    [Fact]
    public async Task SeedOptionalData_WithTestTenant_CreatesWorkOrdersInEveryStatus()
    {
        await _fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithTestTenantOptions);

        var orders = await _fixture.LoadSeededWorkOrdersAsync(DatabaseTestData.TestTenantCompanyId);

        orders.Select(order => order.Status)
            .Should()
            .BeEquivalentTo(
            [
                WorkOrderStatus.Backlog,
                WorkOrderStatus.Staged,
                WorkOrderStatus.Staged,
                WorkOrderStatus.InProgress,
                WorkOrderStatus.Blocked,
                WorkOrderStatus.Completed,
            ]);
        orders.Where(order => order.Status == WorkOrderStatus.Backlog)
            .Should()
            .ContainSingle()
            .Which.AssigneeId.Should()
            .BeNull();
    }

    [Fact]
    public async Task SeedOptionalData_WithTestTenant_PlacesEveryScheduledOrderInsideABlockOfItsAssignee()
    {
        await _fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithTestTenantOptions);
        var orders = await _fixture.LoadSeededWorkOrdersAsync(DatabaseTestData.TestTenantCompanyId);
        var blocks = await _fixture.LoadSeededShiftBlocksAsync(DatabaseTestData.TestTenantCompanyId);

        var scheduled = orders.Where(order => order.AssigneeId is not null).ToList();
        var fittingCount = scheduled.Count(order => blocks.Any(block =>
            block.EmployeeId == order.AssigneeId
            && block.Weekday == Weekday.From(order.PlannedStartLocal!.Value.DayOfWeek)
            && block.Start <= TimeOnly.FromDateTime(order.PlannedStartLocal.Value)
            && TimeOnly.FromDateTime(order.PlannedStartLocal.Value + order.Estimate) <= block.End));

        scheduled.Should().HaveCount(DatabaseTestData.SeededWorkOrderCount - 1);
        fittingCount.Should().Be(scheduled.Count);
        blocks.GroupBy(block => block.EmployeeId)
            .Should()
            .HaveCount(expected: 2)
            .And.OnlyContain(group => group.Count() == DatabaseTestData.SeededShiftBlocksPerEmployee);
    }

    [Fact]
    public async Task SeedOptionalData_ForcedTwice_ReplacesWorkOrdersAndShiftBlocks()
    {
        await _fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithTestTenantOptions);

        await _fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithTestTenantOptions);

        var orders = await _fixture.LoadSeededWorkOrdersAsync(DatabaseTestData.TestTenantCompanyId);
        var blocks = await _fixture.LoadSeededShiftBlocksAsync(DatabaseTestData.TestTenantCompanyId);

        orders.Should().HaveCount(DatabaseTestData.SeededWorkOrderCount);
        blocks.Should().HaveCount(2 * DatabaseTestData.SeededShiftBlocksPerEmployee);
    }
}
