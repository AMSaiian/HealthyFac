using HealthyFac.Core.Messaging.WorkOrders;
using HealthyFac.Insights.Application.Cases;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Agents.ToolCalls;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Insights.IntegrationTests.Cases.Data;
using HealthyFac.Insights.IntegrationTests.Database;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Insights.IntegrationTests.Cases;

[Collection(InsightsCollection.Name)]
public sealed class CaseReadTests(InsightsFixture fixture)
{
    private const string FinalText = "The pressure rises for three days.";
    private const string ProposalTitle = "Replace the filter";

    private static readonly Guid CreatedWorkOrderId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000b01");

    private static readonly Guid OtherCompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-0000000000ff");

    private readonly InsightsFixture _fixture = fixture;

    [Fact]
    public async Task GetCase_WithACommittedProposal_ReturnsTheDraftOutcome()
    {
        var maintenanceCase = CaseTestData.CreateOpenCase();
        await _fixture.SaveCaseWithSignalsAsync(maintenanceCase, (SignalState.Open, 0));
        var run = await _fixture.SaveCompletedRunAsync(maintenanceCase.Id, AgentRole.Planner, verdict: null, FinalText);
        var draftId = await _fixture.SaveCommittedProposalAsync(run.Id, ProposalTitle);

        var response = await _fixture.GetCaseUseCaseAsync(CaseTestData.CompanyId, maintenanceCase.Id);

        response.Outcome.Kind.Should().Be(CaseOutcomeKind.ProposedWorkOrder);
        response.Outcome.WorkOrderDraftId.Should().Be(draftId);
        response.Outcome.Title.Should().Be(ProposalTitle);
        response.Outcome.Reason.Should().Be(FinalText);
        response.SignalCount.Should().Be(expected: 1);
    }

    [Fact]
    public async Task GetCase_AfterTheDraftBecameAWorkOrder_ReturnsWorkOrderCreatedWithTheWorkOrderId()
    {
        var maintenanceCase = CaseTestData.CreateOpenCase();
        await _fixture.SaveCaseWithSignalsAsync(maintenanceCase, (SignalState.Open, 0));
        var run = await _fixture.SaveCompletedRunAsync(maintenanceCase.Id, AgentRole.Planner, verdict: null, FinalText);
        var draftId = await _fixture.SaveCommittedProposalAsync(run.Id, ProposalTitle);
        await _fixture.HandleDraftAppliedAsync(
            new WorkOrderDraftApplied
            {
                DraftId = draftId,
                WorkOrderId = CreatedWorkOrderId,
                CaseId = maintenanceCase.Id,
                CompanyId = CaseTestData.CompanyId,
                AppliedAt = CaseTestData.Now,
            });

        var response = await _fixture.GetCaseUseCaseAsync(CaseTestData.CompanyId, maintenanceCase.Id);

        response.Outcome.Kind.Should().Be(CaseOutcomeKind.WorkOrderCreated);
        response.Outcome.WorkOrderId.Should().Be(CreatedWorkOrderId);
        response.Outcome.Title.Should().Be(ProposalTitle);
    }

    [Fact]
    public async Task GetCase_AfterTheDraftWasDismissed_ReturnsProposalDismissed()
    {
        var maintenanceCase = CaseTestData.CreateOpenCase();
        await _fixture.SaveCaseWithSignalsAsync(maintenanceCase, (SignalState.Open, 0));
        var run = await _fixture.SaveCompletedRunAsync(maintenanceCase.Id, AgentRole.Planner, verdict: null, FinalText);
        var draftId = await _fixture.SaveCommittedProposalAsync(run.Id, ProposalTitle);
        await _fixture.HandleDraftDismissedAsync(
            new WorkOrderDraftDismissed
            {
                DraftId = draftId,
                CaseId = maintenanceCase.Id,
                CompanyId = CaseTestData.CompanyId,
                DismissedAt = CaseTestData.Now,
            });

        var response = await _fixture.GetCaseUseCaseAsync(CaseTestData.CompanyId, maintenanceCase.Id);

        response.Outcome.Kind.Should().Be(CaseOutcomeKind.ProposalDismissed);
        response.Outcome.WorkOrderId.Should().BeNull();
    }

    [Fact]
    public async Task GetCase_WithAWatchVerdictAndNoAction_ReturnsWatched()
    {
        var maintenanceCase = CaseTestData.CreateOpenCase();
        await _fixture.SaveCaseWithSignalsAsync(maintenanceCase, (SignalState.Open, 0));
        await _fixture.SaveCompletedRunAsync(maintenanceCase.Id, AgentRole.Triage, AgentRunVerdict.Watch, FinalText);

        var response = await _fixture.GetCaseUseCaseAsync(CaseTestData.CompanyId, maintenanceCase.Id);

        response.Outcome.Kind.Should().Be(CaseOutcomeKind.Watched);
        response.Outcome.Reason.Should().Be(FinalText);
    }

    [Fact]
    public async Task GetCase_WithoutAnyRun_ReturnsNone()
    {
        var maintenanceCase = CaseTestData.CreateOpenCase();
        await _fixture.SaveCaseWithSignalsAsync(maintenanceCase, (SignalState.Open, 0));

        var response = await _fixture.GetCaseUseCaseAsync(CaseTestData.CompanyId, maintenanceCase.Id);

        response.Outcome.Kind.Should().Be(CaseOutcomeKind.None);
    }

    [Fact]
    public async Task SearchCaseSignals_WithOpenAndResolvedSignals_OrdersOpenFirstThenNewestAndPages()
    {
        var maintenanceCase = CaseTestData.CreateOpenCase();
        var ids = await _fixture.SaveCaseWithSignalsAsync(
            maintenanceCase,
            (SignalState.Resolved, 30),
            (SignalState.Open, 10),
            (SignalState.Open, 20),
            (SignalState.Resolved, 5));

        var first = await _fixture.SearchCaseSignalsUseCaseAsync(
            CaseTestData.CompanyId, maintenanceCase.Id, offset: 0, limit: 3);
        var second = await _fixture.SearchCaseSignalsUseCaseAsync(
            CaseTestData.CompanyId, maintenanceCase.Id, offset: 3, limit: 3);

        first.TotalCount.Should().Be(expected: 4);
        first.Items.Select(item => item.Id).Should().Equal(ids[index: 2], ids[index: 1], ids[index: 0]);
        second.Items.Select(item => item.Id).Should().Equal(ids[index: 3]);
    }

    [Fact]
    public async Task SearchCaseRuns_WithThreeRuns_ReturnsTheNewestFirstAndPages()
    {
        var maintenanceCase = CaseTestData.CreateOpenCase();
        await _fixture.SaveCaseWithSignalsAsync(maintenanceCase, (SignalState.Open, 0));
        var oldest = await _fixture.SaveCompletedRunAsync(
            maintenanceCase.Id, AgentRole.Triage, AgentRunVerdict.Escalate, reason: null);
        var middle = await _fixture.SaveCompletedRunAsync(
            maintenanceCase.Id, AgentRole.Planner, verdict: null, reason: null);
        var newest = await _fixture.SaveCompletedRunAsync(
            maintenanceCase.Id, AgentRole.Planner, verdict: null, reason: null);

        var first = await _fixture.SearchCaseRunsUseCaseAsync(
            CaseTestData.CompanyId, maintenanceCase.Id, offset: 0, limit: 2);
        var second = await _fixture.SearchCaseRunsUseCaseAsync(
            CaseTestData.CompanyId, maintenanceCase.Id, offset: 2, limit: 2);

        first.TotalCount.Should().Be(expected: 3);
        first.Items.Select(item => item.Id).Should().Equal(newest.Id, middle.Id);
        second.Items.Select(item => item.Id).Should().Equal(oldest.Id);
    }

    [Fact]
    public async Task GetRunToolCalls_WithTwoCalls_ReturnsThemInTheOrderOfTheirSequence()
    {
        var maintenanceCase = CaseTestData.CreateOpenCase();
        await _fixture.SaveCaseWithSignalsAsync(maintenanceCase, (SignalState.Open, 0));
        var run = await _fixture.SaveCompletedRunAsync(
            maintenanceCase.Id, AgentRole.Planner, verdict: null, reason: null);
        await _fixture.SaveAsync(
            CaseTestData.CreateToolCall(run.Id, sequence: 2),
            CaseTestData.CreateToolCall(run.Id, sequence: 1));

        var response = await _fixture.GetRunToolCallsUseCaseAsync(CaseTestData.CompanyId, maintenanceCase.Id, run.Id);

        response.ToolCalls.Select(call => call.Sequence).Should().Equal(1, 2);
        response.ToolCalls.Should().OnlyContain(call => call.Outcome == ToolCallOutcome.Succeeded);
    }

    [Fact]
    public async Task GetRunToolCalls_WithARunOfAnotherCase_ThrowsNotFoundException()
    {
        var maintenanceCase = CaseTestData.CreateOpenCase();
        await _fixture.SaveCaseWithSignalsAsync(maintenanceCase, (SignalState.Open, 0));
        var otherCase = CaseTestData.CreateOpenCase(CaseTestData.OtherMachineId);
        await _fixture.SaveCaseWithSignalsAsync(otherCase, (SignalState.Open, 0));
        var run = await _fixture.SaveCompletedRunAsync(otherCase.Id, AgentRole.Planner, verdict: null, reason: null);

        var act = () => _fixture.GetRunToolCallsUseCaseAsync(CaseTestData.CompanyId, maintenanceCase.Id, run.Id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CaseReads_WithAnotherCompany_ThrowNotFoundException()
    {
        var maintenanceCase = CaseTestData.CreateOpenCase();
        await _fixture.SaveCaseWithSignalsAsync(maintenanceCase, (SignalState.Open, 0));
        var run = await _fixture.SaveCompletedRunAsync(
            maintenanceCase.Id, AgentRole.Planner, verdict: null, reason: null);

        var header = () => _fixture.GetCaseUseCaseAsync(OtherCompanyId, maintenanceCase.Id);
        var signals = () =>
            _fixture.SearchCaseSignalsUseCaseAsync(OtherCompanyId, maintenanceCase.Id, offset: 0, limit: 10);
        var runs = () => _fixture.SearchCaseRunsUseCaseAsync(OtherCompanyId, maintenanceCase.Id, offset: 0, limit: 10);
        var toolCalls = () => _fixture.GetRunToolCallsUseCaseAsync(OtherCompanyId, maintenanceCase.Id, run.Id);

        await header.Should().ThrowAsync<NotFoundException>();
        await signals.Should().ThrowAsync<NotFoundException>();
        await runs.Should().ThrowAsync<NotFoundException>();
        await toolCalls.Should().ThrowAsync<NotFoundException>();
    }
}
