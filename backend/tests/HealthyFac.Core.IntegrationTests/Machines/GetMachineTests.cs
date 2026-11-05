using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.IntegrationTests.Machines;

public sealed class GetMachineTests(CoreDatabaseFixture fixture) : IClassFixture<CoreDatabaseFixture>
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task GetMachine_InOwnDepartment_ReturnsMachine()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            tenant.FirstDepartmentId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber,
            MachineTestData.LinkDescription);

        var machine = await _fixture.GetMachineAsync(tenant.CompanyId, [tenant.FirstDepartmentId], machineId);

        machine.Id.Should().Be(machineId);
        machine.Name.Should().Be(MachineTestData.MachineName);
        machine.SerialNumber.Should().Be(MachineTestData.SerialNumber);
        machine.DepartmentId.Should().Be(tenant.FirstDepartmentId);
        machine.Description.Should().Be(MachineTestData.LinkDescription);
    }

    [Fact]
    public async Task GetMachine_WithRowInsertedOutOfOrder_ReturnsStateOfLatestStartedAt()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeSeededMachineAsync(tenant);
        await _fixture.AddStateRowAsync(
            machineId,
            MachineState.InMaintenance,
            MachineTestData.FirstCreatedAt.AddDays(MachineTestData.LaterStateDays));
        await _fixture.AddStateRowAsync(
            machineId,
            MachineState.OutOfService,
            MachineTestData.FirstCreatedAt.AddDays(MachineTestData.EarlierStateDays));

        var machine = await _fixture.GetMachineAsync(tenant.CompanyId, [tenant.FirstDepartmentId], machineId);

        machine.State.Should().Be(MachineState.InMaintenance);
    }

    [Fact]
    public async Task GetMachine_WithDeletedLatestRow_ReturnsStateOfLatestActiveRow()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeSeededMachineAsync(tenant);
        var latestStartedAt = MachineTestData.FirstCreatedAt.AddDays(MachineTestData.LaterStateDays);
        await _fixture.AddStateRowAsync(machineId, MachineState.InMaintenance, latestStartedAt);
        await _fixture.SoftDeleteStateRowAsync(machineId, latestStartedAt);

        var machine = await _fixture.GetMachineAsync(tenant.CompanyId, [tenant.FirstDepartmentId], machineId);

        machine.State.Should().Be(MachineState.Operational);
    }

    [Fact]
    public async Task GetMachine_InOtherCompany_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var otherTenant = await _fixture.ArrangeMachineTenantAsync(MachineTestData.OtherCompanyName);
        var otherMachineId = await _fixture.CreateMachineAsync(
            otherTenant.CompanyId,
            otherTenant.BothDepartmentIds,
            otherTenant.FirstDepartmentId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber);

        var act = () => _fixture.GetMachineAsync(tenant.CompanyId, tenant.BothDepartmentIds, otherMachineId);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineErrors.MachineNotFound);
    }

    [Fact]
    public async Task GetMachine_InForeignDepartment_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            tenant.FirstDepartmentId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber);

        var act = () => _fixture.GetMachineAsync(tenant.CompanyId, [tenant.SecondDepartmentId], machineId);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineErrors.MachineNotFound);
    }

    private async Task<Guid> ArrangeSeededMachineAsync(MachineTenant tenant)
    {
        var ids = await _fixture.SeedMachinesAsync(
            tenant.CompanyId,
            tenant.FirstDepartmentId,
            MachineTestData.CreateSingleMachineSeed());

        return ids[index: 0];
    }
}
