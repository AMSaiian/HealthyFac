using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.UnitTests.Drafts.Data;

namespace HealthyFac.Core.UnitTests.Drafts;

public sealed class MachineDraftTests
{
    [Fact]
    public void Dismiss_Twice_Throws()
    {
        var draft = DraftTestData.CreateMachineDraft();
        draft.Dismiss();

        var act = draft.Dismiss;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkApplied_AfterDismiss_Throws()
    {
        var draft = DraftTestData.CreateMachineDraft();
        draft.Dismiss();

        var act = () => draft.MarkApplied(DraftTestData.MachineId);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkApplied_OnPendingDraft_StoresTheMachine()
    {
        var draft = DraftTestData.CreateMachineDraft();

        draft.MarkApplied(DraftTestData.MachineId);

        draft.State.Should().Be(DraftState.Applied);
        draft.AppliedMachineId.Should().Be(DraftTestData.MachineId);
    }

    [Fact]
    public void IsCreate_WithoutTarget_IsTrueAndWithTargetIsFalse()
    {
        var create = DraftTestData.CreateMachineDraft();
        var update = DraftTestData.CreateMachineDraft(DraftTestData.MachineId);

        (create.IsCreate, update.IsCreate).Should().Be((true, false));
    }
}
