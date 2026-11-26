using HealthyFac.Core.Application.Drafts.SearchMachineDrafts;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Drafts.Data;
using HealthyFac.Core.IntegrationTests.Machines;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Shared.Application.Grids;

namespace HealthyFac.Core.IntegrationTests.Drafts;

[Collection(CoreMachinesCollection.Name)]
public sealed class SearchMachineDraftsTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Theory]
    [MemberData(nameof(DraftSearchTestData.Sorts), MemberType = typeof(DraftSearchTestData))]
    public async Task Search_ReadingEveryPage_ReturnsEveryMachineDraftOnceInTheSortedOrder(
        string caseName,
        GridSort<MachineDraftSortColumn> sort,
        Func<IReadOnlyList<MachineDraft>, IEnumerable<Guid>> expectedOrder
    )
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        var draftIds = new List<Guid>();

        for (var index = 0; index < DraftSearchTestData.DraftCount; index++)
        {
            draftIds.Add(await _fixture.AddMachineDraftAsync(tenant.CompanyId, machineId));
        }

        foreach (var draftId in draftIds.Take(DraftSearchTestData.DismissedCount))
        {
            await _fixture.DismissDraftAsync(tenant.CompanyId, draftId);
        }

        var stored = await _fixture.LoadMachineDraftsAsync();
        var seenIds = await _fixture.ReadAllDraftPagesAsync(tenant.CompanyId, machineId, sort);

        seenIds.Should().Equal(expectedOrder(stored), caseName);
        seenIds.Should().HaveCount(draftIds.Count);
    }

    [Fact]
    public async Task Search_WithoutFilter_ReturnsFirstPageOfAllRows()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        var draftIds = new List<Guid>();

        for (var index = 0; index < DraftSearchTestData.NoFilterDraftCount; index++)
        {
            draftIds.Add(await _fixture.AddMachineDraftAsync(tenant.CompanyId, machineId));
        }

        var seenIds = await _fixture.ReadFirstDraftPageWithoutFilterAsync(tenant.CompanyId, machineId);

        seenIds.Should().BeEquivalentTo(draftIds);
        seenIds.Select(id => id.ToString()).Should().BeInAscendingOrder(StringComparer.Ordinal);
    }
}
