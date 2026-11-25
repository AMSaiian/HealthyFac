using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Insights.UnitTests.Messaging.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Insights.UnitTests.Messaging;

[Feature(TestFeatures.Messaging)]
[Feature(TestFeatures.Ingest)]
[Feature(TestFeatures.Drafts)]
public sealed class DraftsProposedFlatteningTests
{
    [Fact]
    public void Flatten_ForAMachineFileWithOnlyDefinitions_SendsNoMachineAndTheDefinitionsWithTheFileMachine()
    {
        var group = DraftsProposedFlatteningTestData.Group(isWithValue: false, isWithDefinitions: true);

        var machines = DraftsProposedFlattening.Machines([group], isMachineFile: true);
        var metrics = DraftsProposedFlattening.Metrics([group], isMachineFile: true);

        machines.Should().BeEmpty();
        var metric = metrics.Should().ContainSingle().Subject;
        metric.MachineId.Should().Be(DraftsProposedFlatteningTestData.FileMachineId);
        metric.MachineDraftId.Should().BeNull();
    }

    [Fact]
    public void Flatten_ForAMachineFileWithAValue_SendsTheUpdateDraftOfTheFileMachine()
    {
        var group = DraftsProposedFlatteningTestData.Group(isWithValue: true, isWithDefinitions: true);

        var machines = DraftsProposedFlattening.Machines([group], isMachineFile: true);

        var machine = machines.Should().ContainSingle().Subject;
        machine.TargetMachineId.Should().Be(DraftsProposedFlatteningTestData.FileMachineId);
        machine.Name.Should().Be(DraftsProposedFlatteningTestData.MachineName);
    }

    [Fact]
    public void Flatten_ForACompanyFileWithOnlyDefinitions_SendsACreateDraftAndTheDefinitionsWithItsDraftId()
    {
        var group = DraftsProposedFlatteningTestData.Group(isWithValue: false, isWithDefinitions: true);

        var machines = DraftsProposedFlattening.Machines([group], isMachineFile: false);
        var statuses = DraftsProposedFlattening.Statuses([group], isMachineFile: false);
        var phases = DraftsProposedFlattening.Phases([group], isMachineFile: false);

        var machine = machines.Should().ContainSingle().Subject;
        machine.TargetMachineId.Should().BeNull();
        statuses.Should().ContainSingle().Which.MachineDraftId.Should().Be(group.DraftId);
        phases.Should().ContainSingle().Which.MachineId.Should().BeNull();
    }

    [Fact]
    public void Flatten_ForACompanyFileWithNoValueAndNoDefinition_LeavesTheMachineOut()
    {
        var group = DraftsProposedFlatteningTestData.Group(isWithValue: false, isWithDefinitions: false);

        var machines = DraftsProposedFlattening.Machines([group], isMachineFile: false);

        machines.Should().BeEmpty();
    }
}
