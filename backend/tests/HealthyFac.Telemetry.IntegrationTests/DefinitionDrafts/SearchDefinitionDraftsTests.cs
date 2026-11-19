using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Filters;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Telemetry.Application.DefinitionDrafts;
using HealthyFac.Telemetry.Application.DefinitionDrafts.SearchDefinitionDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.MetricDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.StatusDrafts;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts;

[Collection(TelemetryDefinitionDraftsCollection.Name)]
public sealed class SearchDefinitionDraftsTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Theory]
    [MemberData(nameof(DefinitionDraftSearchTestData.Sorts), MemberType = typeof(DefinitionDraftSearchTestData))]
    public async Task Search_ReadingEveryPage_ReturnsEveryDraftOnceInTheSortedOrder(
        string caseName,
        GridSort<DefinitionDraftSortColumn> sort,
        Func<IReadOnlyList<MetricDefinitionDraft>, IReadOnlyList<StatusDefinitionDraft>, IEnumerable<Guid>>
            expectedOrder
    )
    {
        var machineId = await ArrangeFiveDraftsAsync();

        var seenIds = await _fixture.ReadAllDefinitionDraftPagesAsync(
            DefinitionDraftTestData.CompanyId, machineId, sort);

        var metrics = await _fixture.LoadMetricDraftsAsync();
        var statuses = await _fixture.LoadStatusDraftsAsync();

        seenIds.Should().Equal(expectedOrder(metrics, statuses), caseName);
        seenIds.Should().HaveCount(DefinitionDraftSearchTestData.DraftCount);
    }

    [Fact]
    public async Task Search_WithKindFilter_ReturnsOnlyThatKind()
    {
        var machineId = await ArrangeFiveDraftsAsync();
        var filter = new DefinitionDraftGridFilter
        {
            Kinds = new SetFilter<string> { Values = [DefinitionDraftKinds.Status] },
        };

        var page = await _fixture.SearchDefinitionDraftsAsync(
            DefinitionDraftTestData.CompanyId, machineId, attemptId: null, limit: 25, filter: filter);

        page.Items.Should().OnlyContain(item => item.Kind == DefinitionDraftKinds.Status);
        page.TotalCount.Should().Be(expected: 2);
    }

    [Fact]
    public async Task Search_WithStateFilter_ReturnsOnlyThoseStates()
    {
        var machineId = await ArrangeFiveDraftsAsync();
        var filter = new DefinitionDraftGridFilter
        {
            States = new SetFilter<DefinitionDraftState> { Values = [DefinitionDraftState.AwaitingTarget] },
        };

        var page = await _fixture.SearchDefinitionDraftsAsync(
            DefinitionDraftTestData.CompanyId, machineId, attemptId: null, limit: 25, filter: filter);

        page.Items.Should().ContainSingle().Which.State.Should().Be(DefinitionDraftState.AwaitingTarget);
    }

    [Fact]
    public async Task Search_ForAMachineOfAnotherCompany_ThrowsNotFound()
    {
        var foreignMachineId = await _fixture.RegisterTenantMachineAsync(Guid.CreateVersion7());

        var act = () => _fixture.SearchDefinitionDraftsAsync(
            DefinitionDraftTestData.CompanyId, foreignMachineId, attemptId: null, limit: 25);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Search_ForAnAttempt_ReturnsTheDraftsOfTheCompanyOnly()
    {
        await ArrangeFiveDraftsAsync();
        var otherCompanyId = Guid.CreateVersion7();
        var otherMachineId = await _fixture.RegisterTenantMachineAsync(otherCompanyId);
        await _fixture.StoreMetricDraftOnMachineAsync(otherCompanyId, otherMachineId, isApplied: false);

        var page = await _fixture.SearchDefinitionDraftsAsync(
            DefinitionDraftTestData.CompanyId, machineId: null, DefinitionDraftTestData.AttemptId, limit: 25);

        page.TotalCount.Should().Be(DefinitionDraftSearchTestData.DraftCount);
    }

    private async Task<Guid> ArrangeFiveDraftsAsync()
    {
        var companyId = DefinitionDraftTestData.CompanyId;
        var machineId = await _fixture.RegisterTenantMachineAsync(companyId);
        await _fixture.StoreMetricDraftOnMachineAsync(companyId, machineId, isApplied: false);
        await _fixture.StoreMetricDraftOnMachineAsync(companyId, machineId, isApplied: false);
        await _fixture.StoreMetricDraftOnMachineAsync(companyId, machineId, isApplied: true);
        await _fixture.StoreAwaitingStatusDraftOnMachineAsync(companyId, machineId);
        await _fixture.StorePendingStatusDraftOnMachineAsync(companyId, machineId);

        return machineId;
    }
}
