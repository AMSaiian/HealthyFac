using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Seeder.IntegrationTests.Cases.Data;
using HealthyFac.Seeder.IntegrationTests.FullSeed.Data;
using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Seeder.IntegrationTests.Machines.Data;
using HealthyFac.Seeder.IntegrationTests.People.Data;
using HealthyFac.Seeder.IntegrationTests.Relay.Data;
using HealthyFac.Seeder.IntegrationTests.WorkOrders.Data;

namespace HealthyFac.Seeder.IntegrationTests.FullSeed;

[Collection(SeederCollection.Name)]
public sealed class FullSeedTests(SeederFixture fixture)
{
    private readonly SeederFixture _fixture = fixture;

    [Fact]
    public async Task RunAsync_OnASaturday_PassesTheCheckAndSeedsTheCountsOfTheDemo()
    {
        var failures = await _fixture.CheckFullSeedAsync(
            FullSeedTestData.SaturdayAnchor, TestContext.Current.CancellationToken);
        await _fixture.RunFullSeedAsync(TestContext.Current.CancellationToken, FullSeedTestData.SaturdayAnchor);

        var employees = await _fixture.LoadEmployeesAsync();
        var invites = await _fixture.LoadInvitesAsync();
        var templateCount = await _fixture.CountShiftTemplateVersionsAsync();
        var overrides = await _fixture.LoadOverridesAsync();
        var machines = await _fixture.LoadMachinesAsync();
        var machineDrafts = await _fixture.LoadMachineDraftsAsync();
        var workOrderDraft = await _fixture.LoadWorkOrderDraftAsync();
        var workOrders = await _fixture.LoadWorkOrdersAsync();
        var company = await _fixture.LoadCompanyAsync();
        failures.Should().BeEmpty();
        company.Id.Should().Be(PeopleTestData.CompanyId);
        employees.Should()
            .HaveCount(FullSeedTestData.EmployeeCount)
            .And.OnlyContain(employee => employee.Status == EmployeeStatus.Active);
        invites.Count(invite => invite.AcceptedAt is not null).Should().Be(FullSeedTestData.EmployeeCount);
        invites.Count(invite => invite.AcceptedAt is null).Should().Be(FullSeedTestData.OpenInviteCount);
        templateCount.Should().Be(FullSeedTestData.TemplateCount);
        overrides.Should().HaveCount(FullSeedTestData.OverrideCount);
        machines.Should().HaveCount(FullSeedTestData.MachineCount);
        machineDrafts.Should().HaveCount(FullSeedTestData.MachineDraftCount);
        machineDrafts.Count(draft => draft.State == DraftState.Applied)
            .Should()
            .Be(FullSeedTestData.AppliedMachineDraftCount);
        workOrderDraft.Id.Should().Be(CasesTestData.DraftId);
        workOrders.Should().HaveCount(FullSeedTestData.WorkOrderCount);
    }

    [Fact]
    public async Task RunAsync_OnASaturday_LeavesNoOpenOrFailedMessageInAnyDatabase()
    {
        await _fixture.RunFullSeedAsync(TestContext.Current.CancellationToken, FullSeedTestData.SaturdayAnchor);

        var openCount = await _fixture.CountOpenMessagesAsync();
        var failedCount = await _fixture.CountFailedInboxMessagesAsync();
        openCount.Should().Be(expected: 0);
        failedCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task RunAsync_TwiceOnEmptyStacks_DeliversTheSameMessageTypesInTheSameOrder()
    {
        await _fixture.RunFullSeedAsync(TestContext.Current.CancellationToken, FullSeedTestData.SaturdayAnchor);
        var first = await _fixture.LoadDeliveredTypesAsync();
        await _fixture.ResetAsync();

        await _fixture.RunFullSeedAsync(TestContext.Current.CancellationToken, FullSeedTestData.SaturdayAnchor);

        var second = await _fixture.LoadDeliveredTypesAsync();
        second.Should().BeEquivalentTo(first, options => options.WithStrictOrdering());
    }
}
