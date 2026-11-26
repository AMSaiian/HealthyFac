using HealthyFac.MachineSimulator.Scenarios.Curves;
using HealthyFac.MachineSimulator.Scenarios.Modules;
using HealthyFac.MachineSimulator.Scenarios.Presets;
using HealthyFac.MachineSimulator.UnitTests.Scenarios.Data;

namespace HealthyFac.MachineSimulator.UnitTests.Scenarios;

public sealed class CncTurningCenterScenarioTests
{
    private readonly CncTurningCenterScenario _scenario = new();

    [Fact]
    public void Sample_WhileCutting_SendsCuttingValuesAndCode()
    {
        var context = ScenarioContextFactory.Create(_scenario, runSeconds: 10);

        var sample = _scenario.Sample(context);

        sample.IsRunning.Should().BeTrue();
        sample.StatusCodes.Should().Equal(CncTurningCenterScenarioTestData.CuttingCode);
        sample.Values["door.open"].Should().Be(expected: false);
        sample.Values["spindle.mode"].Should().Be(expected: "auto");

        ((double)sample.Values["spindle.speed"]).Should()
            .BeApproximately(expectedValue: 3000, 3000 * ValueNoise.MaxShare);

        ((double)sample.Values["spindle.vibration"]).Should()
            .BeApproximately(expectedValue: 1.1, 1.1 * ValueNoise.MaxShare);
    }

    [Fact]
    public void Sample_DuringPartChange_SendsPartChangeValuesAndCode()
    {
        var context = ScenarioContextFactory.Create(_scenario, runSeconds: 45);

        var sample = _scenario.Sample(context);

        sample.StatusCodes.Should().Equal(CncTurningCenterScenarioTestData.PartChangeCode);
        sample.Values["door.open"].Should().Be(expected: true);
        sample.Values["spindle.speed"].Should().Be(expected: 0d);
    }

    [Theory]
    [MemberData(
        nameof(CncTurningCenterScenarioTestData.SpindleTemperatures),
        MemberType = typeof(CncTurningCenterScenarioTestData))]
    public void Sample_WithDrainingSpindle_FollowsTheTemperatureCurve(string caseName, double progress, double expected)
    {
        var modules = ScenarioContextFactory.ModulesWith(_scenario, ModuleName.Spindle, ModuleMode.Draining, progress);
        var context = ScenarioContextFactory.Create(_scenario, runSeconds: 10, modules);

        var sample = _scenario.Sample(context);

        ((double)sample.Values["spindle.temperature"])
            .Should()
            .BeApproximately(expected, expected * CncTurningCenterScenarioTestData.Tolerance, caseName);
    }

    [Theory]
    [MemberData(
        nameof(CncTurningCenterScenarioTestData.SpindleCodes),
        MemberType = typeof(CncTurningCenterScenarioTestData))]
    public void Sample_WithDrainingSpindle_SendsTheCodeAtItsThreshold(
        string caseName,
        double progress,
        string code,
        bool isSent
    )
    {
        var modules = ScenarioContextFactory.ModulesWith(_scenario, ModuleName.Spindle, ModuleMode.Draining, progress);
        var context = ScenarioContextFactory.Create(_scenario, runSeconds: 10, modules);

        var sample = _scenario.Sample(context);

        sample.StatusCodes.Contains(code).Should().Be(isSent, caseName);
    }

    [Fact]
    public void Sample_WithSpindleFailure_SendsStoppedValuesAndFailureCodesOnly()
    {
        var modules = ScenarioContextFactory.ModulesWith(_scenario, ModuleName.Spindle, ModuleMode.Failure);
        var context = ScenarioContextFactory.Create(_scenario, runSeconds: 10, modules);

        var sample = _scenario.Sample(context);

        sample.IsRunning.Should().BeFalse();
        sample.StatusCodes.Should().BeEquivalentTo("A101", "A111");
        sample.Values["spindle.speed"].Should().Be(expected: 0d);
        sample.Values["spindle.mode"].Should().Be(expected: "off");
    }

    [Fact]
    public void Sample_InMaintenance_SendsStoppedValuesWithoutCodes()
    {
        var modules = ScenarioContextFactory.ModulesWith(_scenario, ModuleName.Spindle, ModuleMode.Failure);
        var context = ScenarioContextFactory.Create(_scenario, runSeconds: 10, modules, isStopped: true);

        var sample = _scenario.Sample(context);

        sample.IsRunning.Should().BeFalse();
        sample.StatusCodes.Should().BeEmpty();
        sample.Values["spindle.speed"].Should().Be(expected: 0d);
    }

    [Fact]
    public void Sample_AfterStop_CoolsTheSpindleSlowlyTowardAmbient()
    {
        var context = ScenarioContextFactory.Create(_scenario, runSeconds: 39);
        _scenario.Sample(context);
        var stopped = ScenarioContextFactory.Create(_scenario, runSeconds: 39, isStopped: true, elapsedSeconds: 10);

        foreach (var pair in context.Memory)
        {
            stopped.Memory[pair.Key] = pair.Value;
        }

        var sample = _scenario.Sample(stopped);

        var before = context.Memory["spindle.temperature"];

        ((double)sample.Values["spindle.temperature"])
            .Should()
            .BeApproximately(before - 0.5, (before - 0.5) * ValueNoise.MaxShare);
    }

    [Theory]
    [MemberData(
        nameof(CncTurningCenterScenarioTestData.ClampedValues),
        MemberType = typeof(CncTurningCenterScenarioTestData))]
    public void Sample_WithADrainNearTheTrip_KeepsTheValueOnTheSideOfTheCurve(
        string caseName,
        ModuleName module,
        string key,
        int runSeconds,
        double lowerBound,
        double upperBound
    )
    {
        var modules = ScenarioContextFactory.ModulesWith(_scenario, module, ModuleMode.Draining, progress: 0.99);
        var context = ScenarioContextFactory.Create(_scenario, runSeconds, modules);

        var sample = _scenario.Sample(context);

        var value = (double)sample.Values[key];
        value.Should().BeInRange(lowerBound, upperBound, caseName);
    }
}
