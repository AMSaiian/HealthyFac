using HealthyFac.Gateway.Web.Composition.Drafts;
using static HealthyFac.Gateway.UnitTests.Composition.Data.AttemptDraftGrouperTestData;

namespace HealthyFac.Gateway.UnitTests.Composition;

public sealed class AttemptDraftGrouperTests
{
    [Fact]
    public void Group_WithDraftsOfATargetMachineAndACreateDraft_PutsEachDraftInItsGroup()
    {
        var metrics = new[] { Draft(id: "t1", targetMachineId: "m1", targetMachineDraftId: null, entryId: "e1") };
        var statuses = new[] { Draft(id: "t2", targetMachineId: null, targetMachineDraftId: "d2", entryId: "e2") };

        var groups = AttemptDraftGrouper.Group([MachineGroup, CreateGroup], metrics, statuses, []);

        groups.Should().HaveCount(expected: 2);
        Ids(groups[index: 0], list: "metrics").Should().Equal("t1");
        Ids(groups[index: 0], list: "statuses").Should().BeEmpty();
        Ids(groups[index: 1], list: "statuses").Should().Equal("t2");
    }

    [Fact]
    public void Group_WithADraftOfAMachineWithoutACoreGroup_MakesAGroupThatNeedsTheMachineName()
    {
        var metrics = new[] { Draft(id: "t1", targetMachineId: "m9", targetMachineDraftId: null, entryId: "e1") };

        var groups = AttemptDraftGrouper.Group([MachineGroup], metrics, [], []);

        groups.Should().HaveCount(expected: 2);
        groups[index: 1].LookupMachineId.Should().Be(expected: "m9");
        groups[index: 1].Group[propertyName: "machineId"]!.GetValue<string>().Should().Be(expected: "m9");
        groups[index: 1].Group[propertyName: "machineDraft"].Should().BeNull();
    }

    [Fact]
    public void Group_WithDraftsThatWaitForATargetOfOneEntry_PutsThemInOneGroupWithoutAMachine()
    {
        var metrics = new[] { Draft(id: "t1", targetMachineId: null, targetMachineDraftId: null, entryId: "e5") };
        var statuses = new[]
        {
            Draft(id: "t2", targetMachineId: null, targetMachineDraftId: null, entryId: "e5"),
            Draft(id: "t3", targetMachineId: null, targetMachineDraftId: null, entryId: "e6"),
        };

        var groups = AttemptDraftGrouper.Group([], metrics, statuses, []);

        groups.Should().HaveCount(expected: 2);
        Ids(groups[index: 0], list: "metrics").Should().Equal("t1");
        Ids(groups[index: 0], list: "statuses").Should().Equal("t2");
        Ids(groups[index: 1], list: "statuses").Should().Equal("t3");
        groups.Should().OnlyContain(group => group.LookupMachineId == null);
        groups.Select(group => group.LookupDraftId).Should().Equal("e5", "e6");
    }

    [Fact]
    public void Group_WithADraftWithoutATargetOfACreateDraftEntry_JoinsTheGroupOfTheCreateDraft()
    {
        var metrics = new[] { Draft(id: "t1", targetMachineId: null, targetMachineDraftId: null, entryId: "d2") };

        var groups = AttemptDraftGrouper.Group([CreateGroup], metrics, [], []);

        groups.Should().ContainSingle();
        Ids(groups[index: 0], list: "metrics").Should().Equal("t1");
    }

    [Fact]
    public void Group_WithAPhaseDraftOfATargetMachine_PutsItInThePhasesOfTheGroupOfTheMachine()
    {
        var phases = new[] { Draft(id: "p1", targetMachineId: "m1", targetMachineDraftId: null, entryId: "e1") };

        var groups = AttemptDraftGrouper.Group([MachineGroup], [], [], phases);

        groups.Should().ContainSingle();
        Ids(groups[index: 0], list: "phases").Should().Equal("p1");
        Ids(groups[index: 0], list: "metrics").Should().BeEmpty();
    }

    private static string[] Ids(AttemptGroup group, string list) =>
        [.. group.Group[list]!.AsArray().Select(item => item![propertyName: "id"]!.GetValue<string>())];
}
