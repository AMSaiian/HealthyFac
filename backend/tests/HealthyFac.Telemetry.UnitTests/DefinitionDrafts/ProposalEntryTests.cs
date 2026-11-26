using HealthyFac.Telemetry.Domain.DefinitionDrafts.ProposalEntries;
using HealthyFac.Telemetry.UnitTests.DefinitionDrafts.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.UnitTests.DefinitionDrafts;

[Feature(TestFeatures.Drafts)]
[Feature(TestFeatures.SignalModel)]
public sealed class ProposalEntryTests
{
    [Theory]
    [MemberData(nameof(ProposalEntryTestData.Transitions), MemberType = typeof(ProposalEntryTestData))]
    public void Move_FromState_ChangesOnlyAllowedMoves(
        string caseName,
        ProposalEntryState start,
        Func<ProposalEntry, bool> move,
        bool expectedIsChanged,
        ProposalEntryState expectedState
    )
    {
        var entry = ProposalEntryTestData.CreateEntry(start);
        var machineIdBefore = entry.MachineId;

        var isChanged = move(entry);

        isChanged.Should().Be(expectedIsChanged, caseName);
        entry.State.Should().Be(expectedState, caseName);

        entry.MachineId.Should()
            .Be(
                expectedState == ProposalEntryState.OnMachine && expectedIsChanged
                    ? ProposalEntryTestData.MachineId
                    : machineIdBefore);
    }
}
