using HealthyFac.Insights.Application.Cases;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.UnitTests.Cases.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Insights.UnitTests.Cases;

[Feature(TestFeatures.Cases)]
public sealed class CaseOutcomesTests
{
    [Fact]
    public void Resolve_WithACommittedProposal_GivesTheDraftIdAndTheTitle()
    {
        var action = CaseOutcomeTestData.CreateProposeAction();

        var outcome = CaseOutcomes.Resolve(
            action, decisionRun: null, isAiUnavailable: false, CaseOutcomeTestData.FinalText);

        outcome.Kind.Should().Be(CaseOutcomeKind.ProposedWorkOrder);
        outcome.WorkOrderDraftId.Should().Be(action.Id);
        outcome.Title.Should().Be(CaseOutcomeTestData.WorkOrderTitle);
        outcome.Reason.Should().Be(CaseOutcomeTestData.FinalText);
    }

    [Fact]
    public void Resolve_WithAnAppliedProposal_GivesWorkOrderCreatedWithTheWorkOrderIdAndTheTitle()
    {
        var action = CaseOutcomeTestData.CreateProposeAction();
        action.FollowApplied(CaseOutcomeTestData.WorkOrderId);

        var outcome = CaseOutcomes.Resolve(action, decisionRun: null, isAiUnavailable: false, reason: null);

        outcome.Kind.Should().Be(CaseOutcomeKind.WorkOrderCreated);
        outcome.WorkOrderId.Should().Be(CaseOutcomeTestData.WorkOrderId);
        outcome.Title.Should().Be(CaseOutcomeTestData.WorkOrderTitle);
    }

    [Fact]
    public void Resolve_WithADismissedProposal_GivesProposalDismissedWithoutALink()
    {
        var action = CaseOutcomeTestData.CreateProposeAction();
        action.FollowDismissed();

        var outcome = CaseOutcomes.Resolve(action, decisionRun: null, isAiUnavailable: false, reason: null);

        outcome.Kind.Should().Be(CaseOutcomeKind.ProposalDismissed);
        outcome.WorkOrderDraftId.Should().BeNull();
        outcome.WorkOrderId.Should().BeNull();
    }

    [Fact]
    public void Resolve_WithAnAppliedProposalThatGetsADismissMessage_KeepsTheAppliedFollowUp()
    {
        var action = CaseOutcomeTestData.CreateProposeAction();
        action.FollowApplied(CaseOutcomeTestData.WorkOrderId);
        action.FollowDismissed();

        var outcome = CaseOutcomes.Resolve(action, decisionRun: null, isAiUnavailable: false, reason: null);

        outcome.Kind.Should().Be(CaseOutcomeKind.WorkOrderCreated);
    }

    [Fact]
    public void Resolve_WithACommittedLink_GivesTheWorkOrderId()
    {
        var outcome = CaseOutcomes.Resolve(
            CaseOutcomeTestData.CreateLinkAction(), decisionRun: null, isAiUnavailable: false, reason: null);

        outcome.Kind.Should().Be(CaseOutcomeKind.LinkedWorkOrder);
        outcome.WorkOrderId.Should().Be(CaseOutcomeTestData.WorkOrderId);
    }

    [Fact]
    public void Resolve_WithACommittedClose_GivesTheCloseReason()
    {
        var outcome = CaseOutcomes.Resolve(
            CaseOutcomeTestData.CreateCloseAction(), decisionRun: null, isAiUnavailable: false, reason: null);

        outcome.Kind.Should().Be(CaseOutcomeKind.Closed);
        outcome.Reason.Should().Be(CaseOutcomeTestData.CloseReason);
    }

    [Fact]
    public void Resolve_WithAnActionAndAnUnavailableAi_KeepsTheAction()
    {
        var outcome = CaseOutcomes.Resolve(
            CaseOutcomeTestData.CreateProposeAction(), decisionRun: null, isAiUnavailable: true, reason: null);

        outcome.Kind.Should().Be(CaseOutcomeKind.ProposedWorkOrder);
    }

    [Fact]
    public void Resolve_WithoutAnActionAndWithAnUnavailableAi_GivesAiUnavailable()
    {
        var run = CaseOutcomeTestData.CreateCompletedRun(AgentRunVerdict.Watch);

        var outcome = CaseOutcomes.Resolve(latestAction: null, run, isAiUnavailable: true, reason: null);

        outcome.Kind.Should().Be(CaseOutcomeKind.AiUnavailable);
    }

    [Fact]
    public void Resolve_WithAWatchVerdict_GivesWatchedWithTheFinalText()
    {
        var run = CaseOutcomeTestData.CreateCompletedRun(AgentRunVerdict.Watch);

        var outcome = CaseOutcomes.Resolve(
            latestAction: null, run, isAiUnavailable: false, CaseOutcomeTestData.FinalText);

        outcome.Kind.Should().Be(CaseOutcomeKind.Watched);
        outcome.Reason.Should().Be(CaseOutcomeTestData.FinalText);
    }

    [Fact]
    public void Resolve_WithADismissVerdict_GivesDismissed()
    {
        var run = CaseOutcomeTestData.CreateCompletedRun(AgentRunVerdict.Dismiss);

        var outcome = CaseOutcomes.Resolve(latestAction: null, run, isAiUnavailable: false, reason: null);

        outcome.Kind.Should().Be(CaseOutcomeKind.Dismissed);
    }

    [Fact]
    public void Resolve_WithANoDecisionError_GivesNoDecision()
    {
        var run = CaseOutcomeTestData.CreateFailedRun(AgentRunErrorCode.NoDecision);

        var outcome = CaseOutcomes.Resolve(latestAction: null, run, isAiUnavailable: false, reason: null);

        outcome.Kind.Should().Be(CaseOutcomeKind.NoDecision);
    }

    [Fact]
    public void Resolve_WithoutAnyRun_GivesNone()
    {
        var outcome = CaseOutcomes.Resolve(latestAction: null, decisionRun: null, isAiUnavailable: false, reason: null);

        outcome.Kind.Should().Be(CaseOutcomeKind.None);
    }
}
