using HealthyFac.Core.Application.MachineErrorCodes.SearchMachineErrorCodes;
using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.MachineErrorCodes.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Filters;
using HealthyFac.Shared.Application.Grids;

namespace HealthyFac.Core.IntegrationTests.MachineErrorCodes;

public sealed class SearchMachineErrorCodesTests(CoreDatabaseFixture fixture) : IClassFixture<CoreDatabaseFixture>
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task SearchMachineErrorCodes_Always_ReturnsOnlyActiveCodesOfTheMachine()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(tenant);
        var otherMachineId = await _fixture.CreateErrorCodeMachineAsync(
            tenant,
            MachineErrorCodeTestData.SecondMachineName,
            MachineErrorCodeTestData.SecondMachineSerialNumber);
        var activeId = await _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.Code);
        var deletedId = await _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.OtherCode);
        await _fixture.RemoveMachineErrorCodeRowAsync(tenant, deletedId);
        await _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            otherMachineId,
            MachineErrorCodeTestData.ThirdCode);

        var response = await _fixture.SearchMachineErrorCodesAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            MachineErrorCodeSearchTestData.CreateSearch(tenant.CompanyId, machineId));

        response.Items.Select(item => item.Id).Should().Equal(activeId);
        response.TotalCount.Should().Be(expected: 1);
    }

    [Fact]
    public async Task SearchMachineErrorCodes_WithCodePrefix_MatchesIgnoringCase()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(tenant);
        var ids = await _fixture.SeedMachineErrorCodesAsync(machineId, MachineErrorCodeSearchTestData.CreateSeeds());
        var filter = new MachineErrorCodeGridFilter
        {
            Code = new TextFilter { Value = MachineErrorCodeSearchTestData.LowerAlphaPrefix },
        };
        var sort = new GridSort<MachineErrorCodeSortColumn>
        {
            Column = MachineErrorCodeSortColumn.Code,
            Direction = SortDirection.Ascending,
        };

        var response = await _fixture.SearchMachineErrorCodesAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            MachineErrorCodeSearchTestData.CreateSearch(tenant.CompanyId, machineId, filter, sort));

        response.Items.Select(item => item.Id).Should().Equal(ids[index: 0], ids[index: 1]);
    }

    [Fact]
    public async Task SearchMachineErrorCodes_WithCreatedRange_ReturnsCodesInRange()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(tenant);
        var ids = await _fixture.SeedMachineErrorCodesAsync(machineId, MachineErrorCodeSearchTestData.CreateSeeds());
        var filter = new MachineErrorCodeGridFilter
        {
            CreatedAt = new DateRangeFilter
            {
                From = MachineErrorCodeSearchTestData.FirstCreatedAt.AddDays(value: 1),
                To = MachineErrorCodeSearchTestData.FirstCreatedAt.AddDays(value: 3),
            },
        };
        var sort = new GridSort<MachineErrorCodeSortColumn>
        {
            Column = MachineErrorCodeSortColumn.Code,
            Direction = SortDirection.Ascending,
        };

        var response = await _fixture.SearchMachineErrorCodesAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            MachineErrorCodeSearchTestData.CreateSearch(tenant.CompanyId, machineId, filter, sort));

        response.Items.Select(item => item.Id).Should().Equal(ids[index: 1], ids[index: 2]);
    }

    [Theory]
    [MemberData(nameof(MachineErrorCodeSearchTestData.SortCases), MemberType = typeof(MachineErrorCodeSearchTestData))]
    public async Task SearchMachineErrorCodes_WithSort_OrdersByColumn(
        MachineErrorCodeSortColumn column,
        SortDirection direction,
        int[] expectedOrder
    )
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(tenant);
        var ids = await _fixture.SeedMachineErrorCodesAsync(machineId, MachineErrorCodeSearchTestData.CreateSeeds());
        var sort = new GridSort<MachineErrorCodeSortColumn> { Column = column, Direction = direction };

        var response = await _fixture.SearchMachineErrorCodesAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            MachineErrorCodeSearchTestData.CreateSearch(tenant.CompanyId, machineId, sort: sort));

        response.Items.Select(item => item.Id).Should().Equal(expectedOrder.Select(index => ids[index]));
    }

    [Fact]
    public async Task SearchMachineErrorCodes_WithTwoPages_ReturnsEveryCodeOnce()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(tenant);
        var ids = await _fixture.SeedMachineErrorCodesAsync(machineId, MachineErrorCodeSearchTestData.CreateSeeds());
        var sort = new GridSort<MachineErrorCodeSortColumn>
        {
            Column = MachineErrorCodeSortColumn.Code,
            Direction = SortDirection.Ascending,
        };

        var firstPage = await _fixture.SearchMachineErrorCodesAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            MachineErrorCodeSearchTestData.CreateSearch(
                tenant.CompanyId,
                machineId,
                sort: sort,
                limit: MachineErrorCodeSearchTestData.PageSize));
        var secondPage = await _fixture.SearchMachineErrorCodesAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            MachineErrorCodeSearchTestData.CreateSearch(
                tenant.CompanyId,
                machineId,
                sort: sort,
                offset: MachineErrorCodeSearchTestData.SecondPageOffset,
                limit: MachineErrorCodeSearchTestData.PageSize));

        firstPage.Items.Concat(secondPage.Items).Select(item => item.Id).Should().Equal(ids);
        secondPage.TotalCount.Should().Be(ids.Count);
    }

    [Fact]
    public async Task SearchMachineErrorCodes_InForeignDepartment_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(tenant);

        var act = () => _fixture.SearchMachineErrorCodesAsync(
            tenant.CompanyId,
            [tenant.SecondDepartmentId],
            MachineErrorCodeSearchTestData.CreateSearch(tenant.CompanyId, machineId));

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineErrors.MachineNotFound);
    }

    [Fact]
    public async Task SearchMachineErrorCodes_ForMachineOfOtherCompany_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var otherTenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(otherTenant);

        var act = () => _fixture.SearchMachineErrorCodesAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            MachineErrorCodeSearchTestData.CreateSearch(tenant.CompanyId, machineId));

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineErrors.MachineNotFound);
    }

    [Fact]
    public async Task SearchMachineErrorCodes_ForDeletedMachine_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(tenant);
        await _fixture.DeleteMachineAsync(tenant.CompanyId, tenant.BothDepartmentIds, machineId);

        var act = () => _fixture.SearchMachineErrorCodesAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            MachineErrorCodeSearchTestData.CreateSearch(tenant.CompanyId, machineId));

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineErrors.MachineNotFound);
    }
}
