using HealthyFac.MachineSimulator.Scenarios.Curves;
using HealthyFac.MachineSimulator.Scenarios.Modules;
using HealthyFac.MachineSimulator.UnitTests.Scenarios.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.MachineSimulator.UnitTests.Scenarios;

[Feature(TestFeatures.Simulation)]
public sealed class ModuleStateTests
{
    [Theory]
    [MemberData(nameof(ModuleStateTestData.ModeChanges), MemberType = typeof(ModuleStateTestData))]
    public void SetMode_FromStartMode_AppliesTheRule(
        string caseName,
        ModuleMode start,
        ModuleMode change,
        ModuleMode expectedMode,
        double expectedProgress
    )
    {
        var module = new ModuleState(ModuleKind.Trip);
        module.SetMode(start);

        module.SetMode(change);

        module.Mode.Should().Be(expectedMode, caseName);
        module.Progress.Should().Be(expectedProgress, caseName);
    }

    [Fact]
    public void SetMode_DrainingWhileDraining_KeepsTheProgress()
    {
        var module = new ModuleState(ModuleKind.Trip);
        module.SetMode(ModuleMode.Draining);
        module.Advance(TimeSpan.FromSeconds(seconds: 90), ModuleStateTestData.DrainDuration);

        module.SetMode(ModuleMode.Draining);

        module.Progress.Should().BeApproximately(expectedValue: 0.5, precision: 1e-9);
    }

    [Theory]
    [MemberData(nameof(ModuleStateTestData.Advances), MemberType = typeof(ModuleStateTestData))]
    public void Advance_FromProgressZero_ReportsTheCrossedMilestones(
        string caseName,
        int seconds,
        DrainMilestone[] expected
    )
    {
        var module = new ModuleState(ModuleKind.Trip);
        module.SetMode(ModuleMode.Draining);

        var milestones = module.Advance(TimeSpan.FromSeconds(seconds), ModuleStateTestData.DrainDuration);

        milestones.Should().Equal(expected, caseName);
    }

    [Fact]
    public void Advance_For180SecondsWithThreeMinuteDrain_SwitchesToFailure()
    {
        var module = new ModuleState(ModuleKind.Trip);
        module.SetMode(ModuleMode.Draining);

        module.Advance(TimeSpan.FromSeconds(seconds: 180), ModuleStateTestData.DrainDuration);

        module.Mode.Should().Be(ModuleMode.Failure);
        module.Progress.Should().Be(expected: 1);
    }

    [Fact]
    public void Advance_PastAMilestone_ReportsItOnce()
    {
        var module = new ModuleState(ModuleKind.Trip);
        module.SetMode(ModuleMode.Draining);
        module.Advance(TimeSpan.FromSeconds(seconds: 100), ModuleStateTestData.DrainDuration);

        var milestones = module.Advance(TimeSpan.FromSeconds(seconds: 10), ModuleStateTestData.DrainDuration);

        milestones.Should().BeEmpty();
    }

    [Fact]
    public void Advance_WhileExpected_ChangesNothing()
    {
        var module = new ModuleState(ModuleKind.Trip);

        var milestones = module.Advance(TimeSpan.FromSeconds(seconds: 100), ModuleStateTestData.DrainDuration);

        milestones.Should().BeEmpty();
        module.Progress.Should().Be(expected: 0);
    }

    [Fact]
    public void Advance_AfterDrainingWithOwnDuration_UsesTheOwnDuration()
    {
        var module = new ModuleState(ModuleKind.Trip);
        module.SetMode(ModuleMode.Draining, ModuleStateTestData.LongDrainDuration);

        module.Advance(TimeSpan.FromHours(hours: 4), ModuleStateTestData.DrainDuration);

        module.Mode.Should().Be(ModuleMode.Draining);
        module.Progress.Should().BeApproximately(expectedValue: 0.5, precision: 1e-9);
    }

    [Fact]
    public void Advance_AfterDrainingWithOwnDuration_FailsAfterTheOwnDuration()
    {
        var module = new ModuleState(ModuleKind.Trip);
        module.SetMode(ModuleMode.Draining, ModuleStateTestData.LongDrainDuration);
        module.Advance(TimeSpan.FromMinutes(minutes: 479), ModuleStateTestData.DrainDuration);

        var milestones = module.Advance(TimeSpan.FromMinutes(minutes: 1), ModuleStateTestData.DrainDuration);

        milestones.Should().Equal(DrainMilestone.Trip);
        module.Mode.Should().Be(ModuleMode.Failure);
    }

    [Fact]
    public void Advance_AfterDrainingWithoutDuration_UsesTheDurationOfAdvance()
    {
        var module = new ModuleState(ModuleKind.Trip);
        module.SetMode(ModuleMode.Draining);

        module.Advance(TimeSpan.FromMinutes(minutes: 3), ModuleStateTestData.DrainDuration);

        module.Mode.Should().Be(ModuleMode.Failure);
    }

    [Theory]
    [MemberData(nameof(ModuleStateTestData.ModesBeforeDraining), MemberType = typeof(ModuleStateTestData))]
    public void Advance_AfterAnotherModeThenDrainingWithoutDuration_UsesTheDurationOfAdvance(
        string caseName,
        ModuleMode previousMode
    )
    {
        var module = new ModuleState(ModuleKind.Trip);
        module.SetMode(ModuleMode.Draining, ModuleStateTestData.LongDrainDuration);
        module.SetMode(previousMode);
        module.SetMode(ModuleMode.Draining);

        module.Advance(TimeSpan.FromMinutes(minutes: 3), ModuleStateTestData.DrainDuration);

        module.Mode.Should().Be(ModuleMode.Failure, caseName);
    }
}
