using HealthyFac.MachineSimulator.Scenarios;
using HealthyFac.MachineSimulator.Scenarios.Machines;
using HealthyFac.MachineSimulator.Scenarios.Modules;
using HealthyFac.MachineSimulator.UnitTests.Scenarios.Data;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Testing.Features;

namespace HealthyFac.MachineSimulator.UnitTests.Scenarios;

[Feature(TestFeatures.Simulation)]
public sealed class GenericScenarioTests
{
    private readonly GenericScenario _scenario = new();

    [Theory]
    [MemberData(nameof(GenericScenarioTestData.Ranges), MemberType = typeof(GenericScenarioTestData))]
    public void RangeOf_WithBounds_GivesTheEdges(
        string caseName,
        double? min,
        double? max,
        double expectedLow,
        double expectedHigh
    )
    {
        var range = GenericScenario.RangeOf(min, max);

        range.Should().Be((expectedLow, expectedHigh), caseName);
    }

    [Theory]
    [MemberData(nameof(GenericScenarioTestData.Ranges), MemberType = typeof(GenericScenarioTestData))]
    public void Sample_WithNumericMetric_StartsInTheMiddleAndStaysInsideTheRange(
        string caseName,
        double? min,
        double? max,
        double low,
        double high
    )
    {
        var context = CreateContext(Numeric(min, max));

        var first = (double)_scenario.Sample(context).Values["m"];

        var values = Enumerable.Range(start: 0, GenericScenarioTestData.MessageCount)
            .Select(_ => (double)_scenario.Sample(context).Values["m"])
            .ToList();

        first.Should().Be(low + (high - low) / 2, caseName);
        values.Should().OnlyContain(value => value >= low && value <= high, caseName);
    }

    [Fact]
    public void Sample_WithNumericMetric_MovesByAtMostOnePercentOfTheRange()
    {
        var context = CreateContext(Numeric(min: 0, max: 100));
        var previous = (double)_scenario.Sample(context).Values["m"];

        var steps = Enumerable.Range(start: 0, GenericScenarioTestData.MessageCount)
            .Select(_ =>
            {
                var next = (double)_scenario.Sample(context).Values["m"];
                var step = Math.Abs(next - previous);
                previous = next;

                return step;
            })
            .ToList();

        steps.Should().OnlyContain(step => step <= 1.0 + 1e-9);
    }

    [Theory]
    [MemberData(nameof(GenericScenarioTestData.Pushes), MemberType = typeof(GenericScenarioTestData))]
    public void Sample_WithRandomAlwaysOneWay_StopsAtTheEdgeOfTheRange(string caseName, double random, double expected)
    {
        var context = CreateContext(Numeric(min: 10, max: 20));

        context = new ScenarioContext
        {
            RunTime = context.RunTime,
            Elapsed = context.Elapsed,
            Modules = context.Modules,
            Counters = context.Counters,
            IsStopped = false,
            Random = new ConstantRandom(random),
            Memory = context.Memory,
            Definitions = context.Definitions,
        };

        var values = Enumerable.Range(start: 0, count: 200)
            .Select(_ => (double)_scenario.Sample(context).Values["m"])
            .ToList();

        values[^1].Should().Be(expected, caseName);
    }

    [Fact]
    public void Sample_WithBooleanAndStateSet_ReturnsFalseAndTheFirstState()
    {
        var context = CreateContext(
            new MetricInfo { Key = "flag", ValueKind = MetricValueKind.Boolean, Min = null, Max = null, States = null },
            new MetricInfo
            {
                Key = "mode", ValueKind = MetricValueKind.StateSet, Min = null, Max = null, States = ["idle", "busy"],
            });

        var sample = _scenario.Sample(context);

        sample.Values["flag"].Should().Be(expected: false);
        sample.Values["mode"].Should().Be(expected: "idle");
        sample.StatusCodes.Should().BeEmpty();
        sample.GrowingCounters.Should().BeEmpty();
    }

    [Fact]
    public void Sample_WithoutDefinitions_SendsNoValues()
    {
        var context = CreateContext();

        var sample = _scenario.Sample(context);

        sample.Values.Should().BeEmpty();
        sample.StatusCodes.Should().BeEmpty();
    }

    private static MetricInfo Numeric(double? min, double? max) =>
        new() { Key = "m", ValueKind = MetricValueKind.Numeric, Min = min, Max = max };

    private ScenarioContext CreateContext(params MetricInfo[] metrics) =>
        new()
        {
            RunTime = TimeSpan.Zero,
            Elapsed = TimeSpan.FromSeconds(seconds: 1),
            Modules = new Dictionary<ModuleName, ModuleState>(),
            Counters = new Dictionary<string, double>(),
            IsStopped = false,
            Random = new Random(ScenarioContextFactory.Seed),
            Memory = new Dictionary<string, double>(),
            Definitions = new MachineDefinitions { Metrics = metrics, StatusCodes = [] },
        };
}
