using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Seeder.IntegrationTests.Cases.Data;
using HealthyFac.Seeder.IntegrationTests.FullSeed.Data;
using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Seeder.IntegrationTests.Machines.Data;
using HealthyFac.Seeder.IntegrationTests.People.Data;
using HealthyFac.Seeder.IntegrationTests.Relay.Data;
using HealthyFac.Seeder.IntegrationTests.TelemetryHistory.Data;
using HealthyFac.Seeder.IntegrationTests.WorkOrders.Data;
using HealthyFac.Seeder.Moments;
using HealthyFac.Testing.Features;

namespace HealthyFac.Seeder.IntegrationTests.FullSeed;

[Collection(SeederCollection.Name)]
[Feature(TestFeatures.Seeding)]
public sealed class FullSeedTests(SeederFixture fixture)
{
    private readonly SeederFixture _fixture = fixture;

    [Theory(Explicit = true)]
    [MemberData(nameof(FullSeedTestData.Anchors), MemberType = typeof(FullSeedTestData))]
    public async Task RunAsync_OnAnAnchorDay_PassesTheCheckAndSeedsTheCountsAndStatusesOfTheDemo(
        string name,
        DateTimeOffset anchor
    )
    {
        var failures = await _fixture.CheckFullSeedAsync(anchor, TestContext.Current.CancellationToken);
        await _fixture.RunFullSeedAsync(TestContext.Current.CancellationToken, anchor);

        var employees = await _fixture.LoadEmployeesAsync();
        var invites = await _fixture.LoadInvitesAsync();
        var templateCount = await _fixture.CountShiftTemplateVersionsAsync();
        var overrides = await _fixture.LoadOverridesAsync();
        var machines = await _fixture.LoadMachinesAsync();
        var machineDrafts = await _fixture.LoadMachineDraftsAsync();
        var workOrderDraft = await _fixture.LoadWorkOrderDraftAsync();
        var workOrders = await _fixture.LoadWorkOrdersAsync();
        var (_, caseSignal) = await _fixture.LoadOpenCaseAsync();
        var company = await _fixture.LoadCompanyAsync();
        var messageCounts = await _fixture.CountMessagesByMachineAsync();
        failures.Should().BeEmpty(name);
        messageCounts.Should().BeEmpty();
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
        caseSignal.MetricKey.Should().Be(CasesTestData.SignalMetricKey);
        caseSignal.Value.Should().Be(CasesTestData.SignalValue);
        caseSignal.Limit.Should().Be(CasesTestData.SignalLimit);
        workOrders.Should().HaveCount(FullSeedTestData.WorkOrderCount);
        var coupling = workOrders.Single(order => order.Title == WorkOrdersTestData.PumpCouplingTitle);

        var completedAt = SeedCalendar.ToUtc(
            RelativeMoment.Parse(WorkOrdersTestData.PumpCouplingCompletedAt),
            anchor,
            TimeZoneInfo.FindSystemTimeZoneById(PeopleTestData.ZoneId));

        coupling.Current.Status.Should().Be(WorkOrderStatus.Completed);
        coupling.Current.CreatedAt.Should().BeCloseTo(completedAt.UtcDateTime, WorkOrdersTestData.MomentTolerance);
        completedAt.UtcDateTime.Should().BeBefore(coupling.Current.PlannedStartUtc!.Value + coupling.Current.Estimate);

        workOrders.Select(order => order.Title)
            .Should()
            .NotContain(
                [TelemetryHistorySeedTestData.LatheWorkOrderTitle, TelemetryHistorySeedTestData.PumpWorkOrderTitle]);

        workOrders.GroupBy(order => order.Current.Status)
            .ToDictionary(group => group.Key, group => group.Count())
            .Should()
            .BeEquivalentTo(WorkOrdersTestData.StatusCounts);
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_OnASaturday_LeavesNoOpenOrFailedMessageInAnyDatabase()
    {
        await _fixture.RunFullSeedAsync(TestContext.Current.CancellationToken, FullSeedTestData.SaturdayAnchor);

        var openCount = await _fixture.CountOpenMessagesAsync();
        var failedCount = await _fixture.CountFailedInboxMessagesAsync();
        openCount.Should().Be(expected: 0);
        failedCount.Should().Be(expected: 0);
    }

    [Fact(Explicit = true)]
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
