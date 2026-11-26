using System.Diagnostics.Metrics;
using HealthyFac.MachineSimulator.Publishing;
using HealthyFac.MachineSimulator.Scenarios;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Instruments = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Simulator.Instruments;
using Tags = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Simulator.Tags;

namespace HealthyFac.MachineSimulator.UnitTests.Publishing;

[Feature(TestFeatures.Simulation)]
public sealed class SimulatorMetricsTests
{
    private readonly IMeterFactory _meterFactory = new ServiceCollection()
        .AddMetrics()
        .BuildServiceProvider()
        .GetRequiredService<IMeterFactory>();

    [Fact]
    public void RecordMessage_WithPublishedAndDropped_CountsEachWithItsTags()
    {
        var metrics = new SimulatorMetrics(_meterFactory);

        using var collector = new MetricCollector<long>(
            _meterFactory,
            ObservabilityConstants.Simulator.Meter,
            Instruments.Messages);

        metrics.RecordMessage(ScenarioKind.CncTurningCenter, isPublished: true);
        metrics.RecordMessage(ScenarioKind.Generic, isPublished: false);

        var measurements = collector.GetMeasurementSnapshot();
        measurements.Should().HaveCount(expected: 2);
        measurements[0].Value.Should().Be(expected: 1);
        measurements[0].Tags[Tags.Scenario].Should().Be(expected: "cncTurningCenter");
        measurements[0].Tags[Tags.Outcome].Should().Be(expected: "published");
        measurements[1].Tags[Tags.Scenario].Should().Be(expected: "generic");
        measurements[1].Tags[Tags.Outcome].Should().Be(expected: "dropped");
        collector.Instrument!.Unit.Should().Be(expected: "{message}");
    }

    [Fact]
    public void RecordPoll_WithDuration_RecordsSecondsWithThePollTag()
    {
        var metrics = new SimulatorMetrics(_meterFactory);

        using var collector = new MetricCollector<double>(
            _meterFactory,
            ObservabilityConstants.Simulator.Meter,
            Instruments.PollDuration);

        metrics.RecordPoll(ObservabilityConstants.Simulator.Polls.State, TimeSpan.FromMilliseconds(milliseconds: 250));

        var measurement = collector.GetMeasurementSnapshot().Should().ContainSingle().Subject;
        measurement.Value.Should().Be(expected: 0.25);
        measurement.Tags[Tags.Poll].Should().Be(expected: "state");
        collector.Instrument!.Unit.Should().Be(expected: "s");
    }

    [Fact]
    public void Gauge_WithSource_ReportsTheCountThatItReads()
    {
        var metrics = new SimulatorMetrics(_meterFactory);
        metrics.SetPublishingSource(() => 3);

        using var collector = new MetricCollector<int>(
            _meterFactory,
            ObservabilityConstants.Simulator.Meter,
            Instruments.Publishing);

        collector.RecordObservableInstruments();

        collector.GetMeasurementSnapshot().Should().ContainSingle().Which.Value.Should().Be(expected: 3);
        collector.Instrument!.Unit.Should().Be(expected: "{machine}");
    }
}
