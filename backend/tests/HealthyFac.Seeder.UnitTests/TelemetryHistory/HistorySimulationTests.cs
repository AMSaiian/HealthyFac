using HealthyFac.Core.Domain.Machines;
using HealthyFac.MachineSimulator.Scenarios.Modules;
using HealthyFac.Seeder.UnitTests.TelemetryHistory.Data;

namespace HealthyFac.Seeder.UnitTests.TelemetryHistory;

public sealed class HistorySimulationTests
{
    private static readonly DateTime Start = HistorySimulationTestData.WindowStart;

    [Fact(Explicit = true)]
    public void Run_WithTheSameSeed_GivesTheSameSamples()
    {
        var first = HistorySimulationTestData.Create(
            HistorySimulationTestData.NoEvents(), HistorySimulationTestData.TenSeconds);

        var second = HistorySimulationTestData.Create(
            HistorySimulationTestData.NoEvents(), HistorySimulationTestData.TenSeconds);

        var firstSamples = first.Run(Start, Start.AddMinutes(value: 30));
        var secondSamples = second.Run(Start, Start.AddMinutes(value: 30));

        secondSamples.Should().HaveCount(firstSamples.Count);

        secondSamples.Select(sample => sample.SampledAt)
            .Should()
            .Equal(firstSamples.Select(sample => sample.SampledAt));

        secondSamples.Zip(firstSamples)
            .Should()
            .OnlyContain(pair => HistorySimulationTestData.AreEqual(pair.First.Sample, pair.Second.Sample));
    }

    [Fact(Explicit = true)]
    public void Run_WithAStep_ChangesOnlyItsMetricWithinItsMoments()
    {
        var plain = HistorySimulationTestData.Create(
            HistorySimulationTestData.NoEvents(), HistorySimulationTestData.TenSeconds);

        var stepped = HistorySimulationTestData.Create(
            HistorySimulationTestData.Events(
                steps: [HistorySimulationTestData.Step(from: "h-11:50", until: "h-11:40")]),
            HistorySimulationTestData.TenSeconds);

        var plainSamples = plain.Run(Start, Start.AddHours(value: 1));
        var steppedSamples = stepped.Run(Start, Start.AddHours(value: 1));

        var stepStart = Start.AddMinutes(value: 10);
        var stepEnd = Start.AddMinutes(value: 20);

        var inside = steppedSamples.Zip(plainSamples)
            .Where(pair => pair.First.SampledAt >= stepStart && pair.First.SampledAt < stepEnd)
            .ToList();

        var outside = steppedSamples.Zip(plainSamples).Except(inside).ToList();

        inside.Should()
            .NotBeEmpty()
            .And.OnlyContain(pair =>
                HistorySimulationTestData.AreEqual(
                    pair.First.Sample, pair.Second.Sample, HistorySimulationTestData.CabinetMetric)
                && (double)pair.First.Sample.Values[HistorySimulationTestData.CabinetMetric]
                == (double)pair.Second.Sample.Values[HistorySimulationTestData.CabinetMetric] + 5);

        outside.Should()
            .NotBeEmpty()
            .And.OnlyContain(pair => HistorySimulationTestData.AreEqual(pair.First.Sample, pair.Second.Sample));
    }

    [Fact(Explicit = true)]
    public void Run_WithAnOverride_ReplacesTheValuesAndTheCodesWithinItsMoments()
    {
        var plain = HistorySimulationTestData.Create(
            HistorySimulationTestData.NoEvents(), HistorySimulationTestData.TenSeconds);

        var overridden = HistorySimulationTestData.Create(
            HistorySimulationTestData.Events(
                overrides: [HistorySimulationTestData.Override(from: "h-11:50", until: "h-11:40")]),
            HistorySimulationTestData.TenSeconds);

        var plainSamples = plain.Run(Start, Start.AddHours(value: 1));
        var overriddenSamples = overridden.Run(Start, Start.AddHours(value: 1));

        var overrideStart = Start.AddMinutes(value: 10);
        var overrideEnd = Start.AddMinutes(value: 20);

        var inside = overriddenSamples.Zip(plainSamples)
            .Where(pair => pair.First.SampledAt >= overrideStart && pair.First.SampledAt < overrideEnd)
            .ToList();

        var outside = overriddenSamples.Zip(plainSamples).Except(inside).ToList();

        inside.Should()
            .NotBeEmpty()
            .And.OnlyContain(pair =>
                pair.First.Sample.StatusCodes.SequenceEqual(new[] { HistorySimulationTestData.OverrideCode })
                && Equals(pair.First.Sample.Values[HistorySimulationTestData.CoolantMetric], 42d)
                && Equals(pair.First.Sample.Values[HistorySimulationTestData.DoorMetric], true)
                && HistorySimulationTestData.AreValuesEqual(
                    pair.First.Sample,
                    pair.Second.Sample,
                    HistorySimulationTestData.CoolantMetric,
                    HistorySimulationTestData.DoorMetric));

        outside.Should()
            .NotBeEmpty()
            .And.OnlyContain(pair => HistorySimulationTestData.AreEqual(pair.First.Sample, pair.Second.Sample));
    }

    [Fact(Explicit = true)]
    public void Run_WithAGap_GivesNoSampleWithinAndEqualSamplesAfter()
    {
        var plain = HistorySimulationTestData.Create(
            HistorySimulationTestData.NoEvents(), HistorySimulationTestData.TenSeconds);

        var gapped = HistorySimulationTestData.Create(
            HistorySimulationTestData.Events(
                gaps: [HistorySimulationTestData.Gap(from: "h-11:50", until: "h-11:40")]),
            HistorySimulationTestData.TenSeconds);

        var plainSamples = plain.Run(Start, Start.AddHours(value: 1));
        var gappedSamples = gapped.Run(Start, Start.AddHours(value: 1));

        var gapStart = Start.AddMinutes(value: 10);
        var gapEnd = Start.AddMinutes(value: 20);
        var plainByTime = plainSamples.ToDictionary(sample => sample.SampledAt);
        gappedSamples.Should().NotContain(sample => sample.SampledAt >= gapStart && sample.SampledAt < gapEnd);

        gappedSamples.Should()
            .HaveCount(
                plainSamples.Count
                - plainSamples.Count(sample => sample.SampledAt >= gapStart && sample.SampledAt < gapEnd));

        gappedSamples.Should()
            .OnlyContain(sample =>
                HistorySimulationTestData.AreEqual(sample.Sample, plainByTime[sample.SampledAt].Sample));
    }

    [Fact(Explicit = true)]
    public void Run_WithAnEightHourDrain_ReachesTheAlarmProgressAfterSixHoursAndTwentyFourMinutes()
    {
        var simulation = HistorySimulationTestData.Create(
            HistorySimulationTestData.Events([HistorySimulationTestData.Drain(drainHours: 8)]),
            HistorySimulationTestData.TenSeconds);

        simulation.Run(Start, Start + TimeSpan.FromMinutes(minutes: 384) - HistorySimulationTestData.TenSeconds);

        simulation.Machine.Modules[ModuleName.Spindle]
            .Progress.Should()
            .BeApproximately(expectedValue: 0.8, precision: 1e-6);
    }

    [Fact(Explicit = true)]
    public void Run_WithAMaintenance_GivesStoppedValuesInBetweenAndResetsTheModules()
    {
        var simulation = HistorySimulationTestData.Create(
            HistorySimulationTestData.Events([HistorySimulationTestData.Drain(drainHours: 8)]),
            HistorySimulationTestData.TenSeconds,
            [
                HistorySimulationTestData.StateChange(MachineState.InMaintenance, TimeSpan.FromHours(hours: 1)),
                HistorySimulationTestData.StateChange(
                    MachineState.Operational, TimeSpan.FromMinutes(minutes: 70)),
            ]);

        var samples = simulation.Run(Start, Start.AddHours(value: 2));

        var inMaintenance = samples
            .Where(sample => sample.SampledAt > Start.AddMinutes(value: 61)
                && sample.SampledAt < Start.AddMinutes(value: 69))
            .ToList();

        inMaintenance.Should()
            .NotBeEmpty()
            .And.OnlyContain(sample =>
                !sample.Sample.IsRunning
                && Equals(sample.Sample.Values[HistorySimulationTestData.SpindleSpeedMetric], 0d));

        simulation.Machine.Modules.Values.Should().OnlyContain(module => module.Mode == ModuleMode.Expected);
    }

    [Fact(Explicit = true)]
    public void Run_WithAOneSecondIntervalOverTenMinutes_Gives601SamplesOnWholeSecondsWithBothEndsIncluded()
    {
        var simulation = HistorySimulationTestData.Create(
            HistorySimulationTestData.NoEvents(), HistorySimulationTestData.OneSecond);

        var samples = simulation.Run(Start, Start.AddMinutes(value: 10));

        samples.Should()
            .HaveCount(expected: 601)
            .And.OnlyContain(sample => (sample.SampledAt - Start).Ticks % TimeSpan.TicksPerSecond == 0);

        samples[0].SampledAt.Should().Be(Start);
        samples[^1].SampledAt.Should().Be(Start.AddMinutes(value: 10));
    }

    [Fact(Explicit = true)]
    public void Run_InTwoCalls_GivesTheSamplesOfOneCallWithoutRepeatOrLoss()
    {
        var whole = HistorySimulationTestData.Create(
            HistorySimulationTestData.NoEvents(), HistorySimulationTestData.TenSeconds);

        var split = HistorySimulationTestData.Create(
            HistorySimulationTestData.NoEvents(), HistorySimulationTestData.TenSeconds);

        var middle = Start.AddMinutes(value: 7).AddSeconds(value: 10);

        var wholeSamples = whole.Run(Start, Start.AddMinutes(value: 20));
        var firstPart = split.Run(Start, middle);
        var secondPart = split.Run(middle, Start.AddMinutes(value: 20));

        var splitSamples = firstPart.Concat(secondPart).ToList();

        splitSamples.Select(sample => sample.SampledAt)
            .Should()
            .Equal(wholeSamples.Select(sample => sample.SampledAt));

        splitSamples.Zip(wholeSamples)
            .Should()
            .OnlyContain(pair => HistorySimulationTestData.AreEqual(pair.First.Sample, pair.Second.Sample));
    }
}
