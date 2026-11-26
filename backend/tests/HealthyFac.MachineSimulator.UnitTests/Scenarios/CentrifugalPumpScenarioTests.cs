using HealthyFac.MachineSimulator.Scenarios.Curves;
using HealthyFac.MachineSimulator.Scenarios.Modules;
using HealthyFac.MachineSimulator.Scenarios.Presets;
using HealthyFac.MachineSimulator.UnitTests.Scenarios.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.MachineSimulator.UnitTests.Scenarios;

[Feature(TestFeatures.Simulation)]
public sealed class CentrifugalPumpScenarioTests
{
    private readonly CentrifugalPumpScenario _scenario = new();

    [Fact]
    public void Sample_WhileRunning_SendsRunningValuesAndCode()
    {
        var context = ScenarioContextFactory.Create(_scenario, runSeconds: 10);

        var sample = _scenario.Sample(context);

        sample.IsRunning.Should().BeTrue();
        sample.StatusCodes.Should().Equal("P-22");
        sample.Values["pump.state"].Should().Be(expected: "running");
        sample.Values["seal.leakage"].Should().Be(expected: false);

        ((double)sample.Values["flow.rate"]).Should()
            .BeApproximately(expectedValue: 108, 108 * ValueNoise.MaxShare);

        sample.Values["pump.operating_hours"].Should().Be(expected: 12030d);
    }

    [Theory]
    [MemberData(
        nameof(CentrifugalPumpScenarioTestData.BearingTemperatures),
        MemberType = typeof(CentrifugalPumpScenarioTestData))]
    public void Sample_WithDrainingBearings_FollowsTheDeTemperatureCurve(
        string caseName,
        double progress,
        double expected
    )
    {
        var modules = ScenarioContextFactory.ModulesWith(_scenario, ModuleName.Bearings, ModuleMode.Draining, progress);
        var context = ScenarioContextFactory.Create(_scenario, runSeconds: 10, modules);

        var sample = _scenario.Sample(context);

        ((double)sample.Values["bearing_de.temperature"])
            .Should()
            .BeApproximately(expected, expected * CentrifugalPumpScenarioTestData.Tolerance, caseName);
    }

    [Theory]
    [MemberData(
        nameof(CentrifugalPumpScenarioTestData.BearingCodes),
        MemberType = typeof(CentrifugalPumpScenarioTestData))]
    public void Sample_WithDrainingBearings_SendsTheCodeAboveItsLimit(
        string caseName,
        double progress,
        string code,
        bool isSent
    )
    {
        var modules = ScenarioContextFactory.ModulesWith(_scenario, ModuleName.Bearings, ModuleMode.Draining, progress);
        var context = ScenarioContextFactory.Create(_scenario, runSeconds: 10, modules);

        var sample = _scenario.Sample(context);

        sample.StatusCodes.Contains(code).Should().Be(isSent, caseName);
    }

    [Fact]
    public void Sample_WithSealFailure_KeepsRunningAndSendsLeakage()
    {
        var modules = ScenarioContextFactory.ModulesWith(_scenario, ModuleName.Seal, ModuleMode.Failure);
        var context = ScenarioContextFactory.Create(_scenario, runSeconds: 10, modules);

        var sample = _scenario.Sample(context);

        sample.IsRunning.Should().BeTrue();
        sample.StatusCodes.Should().BeEquivalentTo("P-22", "P-08");
        sample.Values["seal.leakage"].Should().Be(expected: true);
        sample.Values["pump.state"].Should().Be(expected: "running");
    }

    [Fact]
    public void Sample_WithSuctionFailure_StopsThePumpWithFaultAndFailureCode()
    {
        var modules = ScenarioContextFactory.ModulesWith(_scenario, ModuleName.Suction, ModuleMode.Failure);
        var context = ScenarioContextFactory.Create(_scenario, runSeconds: 10, modules);

        var sample = _scenario.Sample(context);

        sample.IsRunning.Should().BeFalse();
        sample.StatusCodes.Should().Equal("P-01");
        sample.Values["pump.state"].Should().Be(expected: "fault");
        sample.Values["flow.rate"].Should().Be(expected: 0d);
        sample.Values["motor.speed"].Should().Be(expected: 0d);
    }

    [Fact]
    public void Sample_WithTwoDrainingModulesOnFlow_AddsTheirOffsets()
    {
        var modules = ScenarioContextFactory.CreateModules(_scenario);

        foreach (var name in new[] { ModuleName.Suction, ModuleName.Discharge })
        {
            modules[name].SetMode(ModuleMode.Draining);
            modules[name].Advance(ScenarioContextFactory.DrainDuration * 0.5, ScenarioContextFactory.DrainDuration);
        }

        var context = ScenarioContextFactory.Create(_scenario, runSeconds: 10, modules);

        var sample = _scenario.Sample(context);

        ((double)sample.Values["flow.rate"]).Should()
            .BeApproximately(108 - 48 - 48, 12 * ValueNoise.MaxShare + 0.6);
    }
}
