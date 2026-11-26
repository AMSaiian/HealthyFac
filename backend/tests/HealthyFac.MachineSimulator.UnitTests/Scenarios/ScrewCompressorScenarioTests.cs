using HealthyFac.MachineSimulator.Scenarios.Curves;
using HealthyFac.MachineSimulator.Scenarios.Modules;
using HealthyFac.MachineSimulator.Scenarios.Presets;
using HealthyFac.MachineSimulator.UnitTests.Scenarios.Data;

namespace HealthyFac.MachineSimulator.UnitTests.Scenarios;

public sealed class ScrewCompressorScenarioTests
{
    private readonly ScrewCompressorScenario _scenario = new();

    [Fact]
    public void Sample_WhileLoaded_SendsLoadedValuesCodeAndBothCounters()
    {
        var context = ScenarioContextFactory.Create(_scenario, runSeconds: 60);

        var sample = _scenario.Sample(context);

        sample.StatusCodes.Should().Equal("I0002");
        sample.Values["compressor.state"].Should().Be(expected: "loaded");
        sample.GrowingCounters.Should().BeEquivalentTo("running.hours", "loaded.hours");

        ((double)sample.Values["vessel.pressure"]).Should()
            .BeApproximately(expectedValue: 7.3, 7.3 * ValueNoise.MaxShare);

        sample.Values["running.hours"].Should().Be(expected: 14010d);
        sample.Values["loaded.hours"].Should().Be(expected: 11860d);
    }

    [Fact]
    public void Sample_WhileStandby_SendsStandbyCodeAndGrowsOnlyRunningHours()
    {
        var context = ScenarioContextFactory.Create(_scenario, runSeconds: 130);

        var sample = _scenario.Sample(context);

        sample.StatusCodes.Should().Equal("I0001");
        sample.Values["compressor.state"].Should().Be(expected: "standby");
        sample.Values["motor.speed"].Should().Be(expected: 0d);
        sample.GrowingCounters.Should().Equal("running.hours");
    }

    [Theory]
    [MemberData(
        nameof(ScrewCompressorScenarioTestData.SeparatorDrops),
        MemberType = typeof(ScrewCompressorScenarioTestData))]
    public void Sample_WithDrainingOilSeparator_FollowsThePressureDropCurve(
        string caseName,
        double progress,
        double expected
    )
    {
        var modules = ScenarioContextFactory.ModulesWith(
            _scenario,
            ModuleName.OilSeparator,
            ModuleMode.Draining,
            progress);

        var context = ScenarioContextFactory.Create(_scenario, runSeconds: 60, modules);

        var sample = _scenario.Sample(context);

        ((double)sample.Values["oil_separator.pressure_drop"])
            .Should()
            .BeApproximately(expected, expected * ScrewCompressorScenarioTestData.Tolerance, caseName);
    }

    [Theory]
    [MemberData(
        nameof(ScrewCompressorScenarioTestData.SeparatorCodes),
        MemberType = typeof(ScrewCompressorScenarioTestData))]
    public void Sample_WithDrainingOilSeparator_SendsW3050AboveItsLimit(string caseName, double progress, bool isSent)
    {
        var modules = ScenarioContextFactory.ModulesWith(
            _scenario,
            ModuleName.OilSeparator,
            ModuleMode.Draining,
            progress);

        var context = ScenarioContextFactory.Create(_scenario, runSeconds: 60, modules);

        var sample = _scenario.Sample(context);

        sample.StatusCodes.Contains(value: "W3050").Should().Be(isSent, caseName);
    }

    [Fact]
    public void Sample_WithConverterFailure_SendsFaultStoppedValuesAndFailureCode()
    {
        var modules = ScenarioContextFactory.ModulesWith(_scenario, ModuleName.Converter, ModuleMode.Failure);
        var context = ScenarioContextFactory.Create(_scenario, runSeconds: 60, modules);

        var sample = _scenario.Sample(context);

        sample.IsRunning.Should().BeFalse();
        sample.StatusCodes.Should().Equal("S4020");
        sample.Values["compressor.state"].Should().Be(expected: "fault");
        sample.Values["motor.speed"].Should().Be(expected: 0d);
        sample.Values["vessel.pressure"].Should().Be(expected: 0d);
    }

    [Fact]
    public void Sample_WithAirFilterFailure_KeepsRunningWithLoadedCodeAndFailureCode()
    {
        var modules = ScenarioContextFactory.ModulesWith(_scenario, ModuleName.AirFilter, ModuleMode.Failure);
        var context = ScenarioContextFactory.Create(_scenario, runSeconds: 60, modules);

        var sample = _scenario.Sample(context);

        sample.IsRunning.Should().BeTrue();
        sample.StatusCodes.Should().BeEquivalentTo("I0002", "W3060");
        sample.Values["compressor.state"].Should().Be(expected: "loaded");

        ((double)sample.Values["air_filter.pressure_drop"]).Should()
            .BeApproximately(expectedValue: 6.0, 6.0 * ScrewCompressorScenarioTestData.Tolerance);
    }

    [Theory]
    [MemberData(
        nameof(ScrewCompressorScenarioTestData.AirFilterDrops),
        MemberType = typeof(ScrewCompressorScenarioTestData))]
    public void Sample_WithDrainingAirFilter_FollowsThePressureDropCurveInKilopascal(
        string caseName,
        double progress,
        double expected
    )
    {
        var modules = ScenarioContextFactory.ModulesWith(
            _scenario,
            ModuleName.AirFilter,
            ModuleMode.Draining,
            progress);

        var context = ScenarioContextFactory.Create(_scenario, runSeconds: 60, modules);

        var sample = _scenario.Sample(context);

        ((double)sample.Values["air_filter.pressure_drop"])
            .Should()
            .BeApproximately(expected, expected * ScrewCompressorScenarioTestData.Tolerance, caseName);
    }

    [Theory]
    [MemberData(
        nameof(ScrewCompressorScenarioTestData.AirFilterCodes),
        MemberType = typeof(ScrewCompressorScenarioTestData))]
    public void Sample_WithDrainingAirFilter_SendsW3060AboveFiveKilopascal(
        string caseName,
        double progress,
        bool isSent
    )
    {
        var modules = ScenarioContextFactory.ModulesWith(
            _scenario,
            ModuleName.AirFilter,
            ModuleMode.Draining,
            progress);

        var context = ScenarioContextFactory.Create(_scenario, runSeconds: 60, modules);

        var sample = _scenario.Sample(context);

        sample.StatusCodes.Contains(value: "W3060").Should().Be(isSent, caseName);
    }
}
