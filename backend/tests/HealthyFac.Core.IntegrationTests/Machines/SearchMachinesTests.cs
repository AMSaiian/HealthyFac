using HealthyFac.Core.Application.Machines.SearchMachines;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Filters;
using HealthyFac.Shared.Application.Grids;

namespace HealthyFac.Core.IntegrationTests.Machines;

public sealed class SearchMachinesTests(CoreDatabaseFixture fixture) : IClassFixture<CoreDatabaseFixture>
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task SearchMachines_Always_ReturnsOnlyActorDepartments()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var firstId = await ArrangeMachineAsync(tenant, tenant.FirstDepartmentId, MachineTestData.SerialNumber);
        await ArrangeMachineAsync(tenant, tenant.SecondDepartmentId, MachineTestData.OtherSerialNumber);

        var response = await _fixture.SearchMachinesAsync(
            tenant.CompanyId,
            [tenant.FirstDepartmentId],
            MachineTestData.CreateSearch(tenant.CompanyId));

        response.Items.Select(item => item.Id).Should().Equal(firstId);
        response.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task SearchMachines_ActorWithoutDepartments_ReturnsEmpty()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        await ArrangeMachineAsync(tenant, tenant.FirstDepartmentId, MachineTestData.SerialNumber);

        var response = await _fixture.SearchMachinesAsync(
            tenant.CompanyId,
            [],
            MachineTestData.CreateSearch(tenant.CompanyId));

        response.Items.Should().BeEmpty();
        response.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task SearchMachines_WithFilterSortAndPage_ReturnsMatchingPage()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var ids = await _fixture.SeedMachinesAsync(
            tenant.CompanyId,
            tenant.FirstDepartmentId,
            MachineTestData.CreateFilterSeeds());
        var filter = new MachineGridFilter
        {
            Name = new TextFilter { Value = MachineTestData.LowerNamePrefix },
            States = new SetFilter<MachineState> { Values = [MachineState.Operational] },
            CreatedAt = new DateRangeFilter
            {
                From = MachineTestData.FirstCreatedAt,
                To = MachineTestData.FirstCreatedAt.AddDays(4),
            },
        };
        var sort = new GridSort<MachineSortColumn>
        {
            Column = MachineSortColumn.Name,
            Direction = SortDirection.Descending,
        };

        var response = await _fixture.SearchMachinesAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            MachineTestData.CreateSearch(
                tenant.CompanyId,
                filter,
                sort,
                MachineTestData.SecondPageOffset,
                MachineTestData.PageSize));

        response.Items.Select(item => item.Id).Should().Equal(ids[1], ids[0]);
        response.TotalCount.Should().Be(4);
    }

    [Fact]
    public async Task SearchMachines_WithForeignDepartmentFilter_ThrowsForbidden()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var filter = new MachineGridFilter
        {
            DepartmentIds = new IdSetFilter { Ids = [tenant.SecondDepartmentId] },
        };

        var act = () => _fixture.SearchMachinesAsync(
            tenant.CompanyId,
            [tenant.FirstDepartmentId],
            MachineTestData.CreateSearch(tenant.CompanyId, filter));

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task SearchMachines_WithOwnDepartmentFilter_ReturnsThatDepartment()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        await ArrangeMachineAsync(tenant, tenant.FirstDepartmentId, MachineTestData.SerialNumber);
        var secondId = await ArrangeMachineAsync(tenant, tenant.SecondDepartmentId, MachineTestData.OtherSerialNumber);
        var filter = new MachineGridFilter
        {
            DepartmentIds = new IdSetFilter { Ids = [tenant.SecondDepartmentId] },
        };

        var response = await _fixture.SearchMachinesAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            MachineTestData.CreateSearch(tenant.CompanyId, filter));

        response.Items.Select(item => item.Id).Should().Equal(secondId);
    }

    [Fact]
    public async Task SearchMachines_Always_HidesDeletedMachines()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var deletedId = await ArrangeMachineAsync(tenant, tenant.FirstDepartmentId, MachineTestData.SerialNumber);
        var keptId = await ArrangeMachineAsync(tenant, tenant.FirstDepartmentId, MachineTestData.OtherSerialNumber);
        await _fixture.DeleteMachineAsync(tenant.CompanyId, tenant.BothDepartmentIds, deletedId);

        var response = await _fixture.SearchMachinesAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            MachineTestData.CreateSearch(tenant.CompanyId));

        response.Items.Select(item => item.Id).Should().Equal(keptId);
        response.TotalCount.Should().Be(1);
    }

    private Task<Guid> ArrangeMachineAsync(MachineTenant tenant, Guid departmentId, string serialNumber) =>
        _fixture.CreateMachineAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            departmentId,
            MachineTestData.MachineName,
            serialNumber);
}
