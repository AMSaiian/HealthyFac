using HealthyFac.Core.UnitTests.Drafts.Data;

namespace HealthyFac.Core.UnitTests.Drafts;

public sealed class ChildDraftTests
{
    [Fact]
    public void RetargetToMachine_OnMachineTargetedMetric_Throws()
    {
        var draft = DraftTestData.CreateMetricForMachine();

        var act = () => draft.RetargetToMachine(DraftTestData.MachineId);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RetargetToMachine_OnMachineTargetedStatus_Throws()
    {
        var draft = DraftTestData.CreateStatusForMachine();

        var act = () => draft.RetargetToMachine(DraftTestData.MachineId);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RetargetToMachine_OnDismissedMetric_Throws()
    {
        var draft = DraftTestData.CreateMetricForMachineDraft();
        draft.Dismiss();

        var act = () => draft.RetargetToMachine(DraftTestData.MachineId);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RetargetToMachine_OnMetricOfMachineDraft_LeavesExactlyOneTarget()
    {
        var draft = DraftTestData.CreateMetricForMachineDraft();

        draft.RetargetToMachine(DraftTestData.MachineId);

        draft.TargetMachineId.Should().Be(DraftTestData.MachineId);
        draft.TargetMachineDraftId.Should().BeNull();
    }

    [Fact]
    public void RetargetToMachine_OnStatusOfMachineDraft_LeavesExactlyOneTarget()
    {
        var draft = DraftTestData.CreateStatusForMachineDraft();

        draft.RetargetToMachine(DraftTestData.MachineId);

        draft.TargetMachineId.Should().Be(DraftTestData.MachineId);
        draft.TargetMachineDraftId.Should().BeNull();
    }

    [Fact]
    public void Dismiss_OnAppliedMetric_Throws()
    {
        var draft = DraftTestData.CreateMetricForMachine();
        draft.MarkApplied();

        var act = draft.Dismiss;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkApplied_OnDismissedStatus_Throws()
    {
        var draft = DraftTestData.CreateStatusForMachine();
        draft.Dismiss();

        var act = draft.MarkApplied;

        act.Should().Throw<InvalidOperationException>();
    }
}
