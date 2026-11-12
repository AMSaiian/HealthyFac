using HealthyFac.Core.Application.Machines.SearchMachines;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Shared.Application.Filters;
using HealthyFac.Shared.Application.Grids;

namespace HealthyFac.Core.IntegrationTests.Machines;

[Collection(CoreMachinesCollection.Name)]
public sealed class SearchMachinesTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task SearchMachines_Always_ReturnsOnlyActorCompany()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var otherTenant = await _fixture.ArrangeMachineTenantAsync(MachineTestData.OtherCompanyName);
        var firstId = await ArrangeMachineAsync(tenant, MachineTestData.SerialNumber);
        await ArrangeMachineAsync(otherTenant, MachineTestData.OtherSerialNumber);

        var response = await _fixture.SearchMachinesAsync(
            tenant.CompanyId,
            MachineTestData.CreateSearch(tenant.CompanyId));

        response.Items.Select(item => item.Id).Should().Equal(firstId);
        response.TotalCount.Should().Be(expected: 1);
    }

    [Fact]
    public async Task SearchMachines_WithOperationalFilter_ReturnsMachineWithPastMaintenanceRow()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineWithPastMaintenanceAsync(tenant);

        var response = await _fixture.SearchMachinesAsync(
            tenant.CompanyId,
            MachineTestData.CreateSearch(tenant.CompanyId, CreateStateFilter(MachineState.Operational)));

        response.Items.Select(item => item.Id).Should().Equal(machineId);
        response.Items[index: 0].State.Should().Be(MachineState.Operational);
    }

    [Fact]
    public async Task SearchMachines_WithMaintenanceFilter_MatchesOnlyMachineInMaintenanceNow()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        await ArrangeMachineWithPastMaintenanceAsync(tenant);
        var inMaintenanceIds = await _fixture.SeedMachinesAsync(
            tenant.CompanyId,
            [
                new MachineSeed(
                    MachineTestData.OtherMachineName,
                    MachineTestData.OtherSerialNumber,
                    MachineState.InMaintenance,
                    MachineTestData.FirstCreatedAt),
            ]);

        var response = await _fixture.SearchMachinesAsync(
            tenant.CompanyId,
            MachineTestData.CreateSearch(tenant.CompanyId, CreateStateFilter(MachineState.InMaintenance)));

        response.Items.Select(item => item.Id).Should().Equal(inMaintenanceIds);
        response.Items[index: 0].State.Should().Be(MachineState.InMaintenance);
    }

    [Fact]
    public async Task SearchMachines_WithFilterSortAndPage_ReturnsMatchingPage()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var ids = await _fixture.SeedMachinesAsync(
            tenant.CompanyId,
            MachineTestData.CreateFilterSeeds());
        var filter = new MachineGridFilter
        {
            Name = new TextFilter { Value = MachineTestData.LowerNamePrefix },
            States = new SetFilter<MachineState> { Values = [MachineState.Operational] },
            CreatedAt = new DateRangeFilter
            {
                From = MachineTestData.FirstCreatedAt,
                To = MachineTestData.FirstCreatedAt.AddDays(value: 4),
            },
        };
        var sort = new GridSort<MachineSortColumn>
        {
            Column = MachineSortColumn.Name,
            Direction = SortDirection.Descending,
        };

        var response = await _fixture.SearchMachinesAsync(
            tenant.CompanyId,
            MachineTestData.CreateSearch(
                tenant.CompanyId,
                filter,
                sort,
                MachineTestData.SecondPageOffset,
                MachineTestData.PageSize));

        response.Items.Select(item => item.Id).Should().Equal(ids[index: 1], ids[index: 0]);
        response.TotalCount.Should().Be(expected: 4);
    }

    [Fact]
    public async Task SearchMachines_Always_HidesDeletedMachines()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var deletedId = await ArrangeMachineAsync(tenant, MachineTestData.SerialNumber);
        var keptId = await ArrangeMachineAsync(tenant, MachineTestData.OtherSerialNumber);
        await _fixture.DeleteMachineAsync(tenant.CompanyId, deletedId);

        var response = await _fixture.SearchMachinesAsync(
            tenant.CompanyId,
            MachineTestData.CreateSearch(tenant.CompanyId));

        response.Items.Select(item => item.Id).Should().Equal(keptId);
        response.TotalCount.Should().Be(expected: 1);
    }

    [Fact]
    public async Task SearchMachines_WithGatewayIdFilter_ReturnsOnlyMachinesWithThatPrefix()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        await _fixture.AddMachineWithGatewayIdAsync(
            tenant.CompanyId,
            MachineTestData.GatewayIdWithPrefix,
            MachineTestData.SerialNumber);
        await _fixture.AddMachineWithGatewayIdAsync(
            tenant.CompanyId,
            MachineTestData.GatewayIdWithoutPrefix,
            MachineTestData.OtherSerialNumber);
        await _fixture.AddMachineWithGatewayIdAsync(
            tenant.CompanyId,
            MachineTestData.GatewayIdWithSecondPrefix,
            MachineTestData.ThirdSerialNumber);
        var filter = new MachineGridFilter
        {
            GatewayId = new TextFilter { Value = MachineTestData.GatewayIdPrefix },
        };

        var response = await _fixture.SearchMachinesAsync(
            tenant.CompanyId,
            MachineTestData.CreateSearch(tenant.CompanyId, filter));

        response.Items.Select(item => item.GatewayId).Should().BeEquivalentTo(
            new[] { MachineTestData.GatewayIdWithPrefix, MachineTestData.GatewayIdWithSecondPrefix });
        response.TotalCount.Should().Be(expected: 2);
    }

    [Fact]
    public async Task SearchMachines_SortedByGatewayId_ReturnsAscendingOrder()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        await _fixture.AddMachineWithGatewayIdAsync(
            tenant.CompanyId,
            MachineTestData.GatewayIdWithoutPrefix,
            MachineTestData.SerialNumber);
        await _fixture.AddMachineWithGatewayIdAsync(
            tenant.CompanyId,
            MachineTestData.GatewayIdWithSecondPrefix,
            MachineTestData.OtherSerialNumber);
        await _fixture.AddMachineWithGatewayIdAsync(
            tenant.CompanyId,
            MachineTestData.GatewayIdWithPrefix,
            MachineTestData.ThirdSerialNumber);
        var sort = new GridSort<MachineSortColumn>
        {
            Column = MachineSortColumn.GatewayId,
            Direction = SortDirection.Ascending,
        };

        var response = await _fixture.SearchMachinesAsync(
            tenant.CompanyId,
            MachineTestData.CreateSearch(tenant.CompanyId, sort: sort));

        response.Items.Select(item => item.GatewayId).Should().Equal(
            MachineTestData.GatewayIdWithPrefix,
            MachineTestData.GatewayIdWithSecondPrefix,
            MachineTestData.GatewayIdWithoutPrefix);
    }

    private Task<Guid> ArrangeMachineAsync(MachineTenant tenant, string serialNumber) =>
        _fixture.CreateMachineAsync(
            tenant.CompanyId,
            MachineTestData.MachineName,
            serialNumber);

    private static MachineGridFilter CreateStateFilter(MachineState state) =>
        new() { States = new SetFilter<MachineState> { Values = [state] } };

    private async Task<Guid> ArrangeMachineWithPastMaintenanceAsync(MachineTenant tenant)
    {
        var ids = await _fixture.SeedMachinesAsync(
            tenant.CompanyId,
            MachineTestData.CreateSingleMachineSeed());
        await _fixture.AddStateRowAsync(
            ids[index: 0],
            MachineState.InMaintenance,
            MachineTestData.FirstCreatedAt.AddDays(MachineTestData.EarlierStateDays));
        await _fixture.AddStateRowAsync(
            ids[index: 0],
            MachineState.Operational,
            MachineTestData.FirstCreatedAt.AddDays(MachineTestData.LaterStateDays));

        return ids[index: 0];
    }
}
