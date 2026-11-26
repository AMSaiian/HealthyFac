using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using HealthyFac.Telemetry.UnitTests.DefinitionDrafts.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.UnitTests.DefinitionDrafts;

[Feature(TestFeatures.Drafts)]
[Feature(TestFeatures.SignalModel)]
public sealed class DefinitionDraftTests
{
    [Theory]
    [MemberData(nameof(DefinitionDraftTestData.FollowCases), MemberType = typeof(DefinitionDraftTestData))]
    public void Follow_OnMetricDraft_MapsTheEntryStateToTheTarget(string caseName, FollowCase followCase)
    {
        var draft = DefinitionDraftTestData.CreateMetric(followCase.ChildState, followCase.TentativeMachineId);
        var entry = ProposalEntryTestData.CreateEntry(followCase.EntryState);

        var isRemovalRequired = draft.Follow(entry, followCase.IsTargetRegistrationDeleted);

        isRemovalRequired.Should().Be(followCase.IsRemovalExpected, caseName);
        draft.State.Should().Be(followCase.ExpectedState, caseName);
        draft.TargetMachineId.Should().Be(followCase.ExpectedMachineId, caseName);
        draft.TargetMachineDraftId.Should().Be(followCase.ExpectedMachineDraftId, caseName);
    }

    [Theory]
    [MemberData(nameof(DefinitionDraftTestData.FollowCases), MemberType = typeof(DefinitionDraftTestData))]
    public void Follow_OnStatusDraft_MapsTheEntryStateToTheTarget(string caseName, FollowCase followCase)
    {
        var draft = DefinitionDraftTestData.CreateStatus(followCase.ChildState, followCase.TentativeMachineId);
        var entry = ProposalEntryTestData.CreateEntry(followCase.EntryState);

        var isRemovalRequired = draft.Follow(entry, followCase.IsTargetRegistrationDeleted);

        isRemovalRequired.Should().Be(followCase.IsRemovalExpected, caseName);
        draft.State.Should().Be(followCase.ExpectedState, caseName);
        draft.TargetMachineId.Should().Be(followCase.ExpectedMachineId, caseName);
        draft.TargetMachineDraftId.Should().Be(followCase.ExpectedMachineDraftId, caseName);
    }

    [Theory]
    [MemberData(nameof(DefinitionDraftTestData.NotPendingStates), MemberType = typeof(DefinitionDraftTestData))]
    public void MarkApplied_FromStateOtherThanPending_Throws(string caseName, DefinitionDraftState state)
    {
        var draft = DefinitionDraftTestData.CreateMetric(state);

        var act = draft.MarkApplied;

        act.Should().Throw<InvalidOperationException>(caseName);
    }

    [Theory]
    [MemberData(nameof(DefinitionDraftTestData.NotOpenStates), MemberType = typeof(DefinitionDraftTestData))]
    public void Dismiss_FromAppliedOrDismissed_Throws(string caseName, DefinitionDraftState state)
    {
        var draft = DefinitionDraftTestData.CreateStatus(state);

        var act = draft.Dismiss;

        act.Should().Throw<InvalidOperationException>(caseName);
    }

    [Fact]
    public void Dismiss_FromAwaitingTarget_DismissesTheDraft()
    {
        var draft = DefinitionDraftTestData.CreateMetric(DefinitionDraftState.AwaitingTarget);

        draft.Dismiss();

        draft.State.Should().Be(DefinitionDraftState.Dismissed);
    }
}
