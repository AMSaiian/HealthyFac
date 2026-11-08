using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Filters;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using HealthyFac.Telemetry.Application.StatusDefinitions.SearchStatusDefinitions;
using HealthyFac.Telemetry.Domain.StatusDefinitions;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Telemetry.IntegrationTests.StatusDefinitions.Data;

namespace HealthyFac.Telemetry.IntegrationTests.StatusDefinitions;

[Collection(TelemetryDefinitionsCollection.Name)]
public sealed class SearchStatusDefinitionsTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task SearchStatusDefinitions_Always_ReturnsOnlyActiveCodesOfTheMachine()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var otherMachineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var activeId = await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            machineId,
            StatusDefinitionTestData.Code);
        var deletedId = await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            machineId,
            StatusDefinitionTestData.OtherCode);
        await _fixture.RemoveStatusDefinitionRowAsync(tenant, deletedId);
        await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            otherMachineId,
            StatusDefinitionTestData.ThirdCode);

        var response = await _fixture.SearchStatusDefinitionsAsync(
            tenant.CompanyId,
            StatusDefinitionSearchTestData.CreateSearch(tenant.CompanyId, machineId));

        response.Items.Select(item => item.Id).Should().Equal(activeId);
        response.TotalCount.Should().Be(expected: 1);
    }

    [Fact]
    public async Task SearchStatusDefinitions_WithCodePrefix_MatchesIgnoringCase()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var ids = await _fixture.SeedStatusDefinitionsAsync(machineId, StatusDefinitionSearchTestData.CreateSeeds());
        var filter = new StatusDefinitionGridFilter
        {
            Code = new TextFilter { Value = StatusDefinitionSearchTestData.LowerAlphaPrefix },
        };
        var sort = new GridSort<StatusDefinitionSortColumn>
        {
            Column = StatusDefinitionSortColumn.Code,
            Direction = SortDirection.Ascending,
        };

        var response = await _fixture.SearchStatusDefinitionsAsync(
            tenant.CompanyId,
            StatusDefinitionSearchTestData.CreateSearch(tenant.CompanyId, machineId, filter, sort));

        response.Items.Select(item => item.Id).Should().Equal(ids[index: 0], ids[index: 1]);
    }

    [Fact]
    public async Task SearchStatusDefinitions_WithCreatedRange_ReturnsCodesInRange()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var ids = await _fixture.SeedStatusDefinitionsAsync(machineId, StatusDefinitionSearchTestData.CreateSeeds());
        var filter = new StatusDefinitionGridFilter
        {
            CreatedAt = new DateRangeFilter
            {
                From = StatusDefinitionSearchTestData.FirstCreatedAt.AddDays(value: 1),
                To = StatusDefinitionSearchTestData.FirstCreatedAt.AddDays(value: 3),
            },
        };
        var sort = new GridSort<StatusDefinitionSortColumn>
        {
            Column = StatusDefinitionSortColumn.Code,
            Direction = SortDirection.Ascending,
        };

        var response = await _fixture.SearchStatusDefinitionsAsync(
            tenant.CompanyId,
            StatusDefinitionSearchTestData.CreateSearch(tenant.CompanyId, machineId, filter, sort));

        response.Items.Select(item => item.Id).Should().Equal(ids[index: 1], ids[index: 2]);
    }

    [Fact]
    public async Task SearchStatusDefinitions_WithSeverityFilter_ReturnsOnlyThoseSeverities()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var ids = await _fixture.SeedStatusDefinitionsAsync(machineId, StatusDefinitionSearchTestData.CreateSeeds());
        var filter = new StatusDefinitionGridFilter
        {
            Severities = new SetFilter<StatusSeverity> { Values = [StatusSeverity.Info, StatusSeverity.Error] },
        };

        var response = await _fixture.SearchStatusDefinitionsAsync(
            tenant.CompanyId,
            StatusDefinitionSearchTestData.CreateSearch(tenant.CompanyId, machineId, filter));

        response.Items.Select(item => item.Id).Should().BeEquivalentTo([ids[index: 1], ids[index: 2], ids[index: 3]]);
    }

    [Fact]
    public async Task SearchStatusDefinitions_SortedBySeverityAscending_OrdersInfoWarningError()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        await _fixture.SeedStatusDefinitionsAsync(machineId, StatusDefinitionSearchTestData.CreateSeeds());
        var sort = new GridSort<StatusDefinitionSortColumn>
        {
            Column = StatusDefinitionSortColumn.Severity,
            Direction = SortDirection.Ascending,
        };

        var response = await _fixture.SearchStatusDefinitionsAsync(
            tenant.CompanyId,
            StatusDefinitionSearchTestData.CreateSearch(tenant.CompanyId, machineId, sort: sort));

        response.Items.Select(item => item.Severity)
            .Should()
            .Equal(StatusDefinitionSearchTestData.SeveritiesInAscendingOrder);
    }

    [Theory]
    [MemberData(nameof(StatusDefinitionSearchTestData.SortCases), MemberType = typeof(StatusDefinitionSearchTestData))]
    public async Task SearchStatusDefinitions_WithSort_OrdersByColumn(
        StatusDefinitionSortColumn column,
        SortDirection direction,
        int[] expectedOrder
    )
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var ids = await _fixture.SeedStatusDefinitionsAsync(machineId, StatusDefinitionSearchTestData.CreateSeeds());
        var sort = new GridSort<StatusDefinitionSortColumn> { Column = column, Direction = direction };

        var response = await _fixture.SearchStatusDefinitionsAsync(
            tenant.CompanyId,
            StatusDefinitionSearchTestData.CreateSearch(tenant.CompanyId, machineId, sort: sort));

        response.Items.Select(item => item.Id).Should().Equal(expectedOrder.Select(index => ids[index]));
    }

    [Fact]
    public async Task SearchStatusDefinitions_WithTwoPages_ReturnsEveryCodeOnce()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var ids = await _fixture.SeedStatusDefinitionsAsync(machineId, StatusDefinitionSearchTestData.CreateSeeds());
        var sort = new GridSort<StatusDefinitionSortColumn>
        {
            Column = StatusDefinitionSortColumn.Code,
            Direction = SortDirection.Ascending,
        };

        var firstPage = await _fixture.SearchStatusDefinitionsAsync(
            tenant.CompanyId,
            StatusDefinitionSearchTestData.CreateSearch(
                tenant.CompanyId,
                machineId,
                sort: sort,
                limit: StatusDefinitionSearchTestData.PageSize));
        var secondPage = await _fixture.SearchStatusDefinitionsAsync(
            tenant.CompanyId,
            StatusDefinitionSearchTestData.CreateSearch(
                tenant.CompanyId,
                machineId,
                sort: sort,
                offset: StatusDefinitionSearchTestData.SecondPageOffset,
                limit: StatusDefinitionSearchTestData.PageSize));

        firstPage.Items.Concat(secondPage.Items).Select(item => item.Id).Should().Equal(ids);
        secondPage.TotalCount.Should().Be(ids.Count);
    }

    [Fact]
    public async Task SearchStatusDefinitions_ForMachineOfOtherCompany_ThrowsNotFound()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var otherTenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(otherTenant);

        var act = () => _fixture.SearchStatusDefinitionsAsync(
            tenant.CompanyId,
            StatusDefinitionSearchTestData.CreateSearch(tenant.CompanyId, machineId));

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineRegistrationErrors.MachineNotFound);
    }

    [Fact]
    public async Task SearchStatusDefinitions_ForDeletedMachine_ThrowsNotFound()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        await _fixture.DeleteMachineAsync(tenant.CompanyId, machineId);

        var act = () => _fixture.SearchStatusDefinitionsAsync(
            tenant.CompanyId,
            StatusDefinitionSearchTestData.CreateSearch(tenant.CompanyId, machineId));

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineRegistrationErrors.MachineNotFound);
    }
}
