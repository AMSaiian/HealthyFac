using HealthyFac.Telemetry.Application.OperatingPhases;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.UnitTests.OperatingPhases;

[Feature(TestFeatures.SignalModel)]
[Feature(TestFeatures.Evaluation)]
public sealed class PhaseTimeCalculatorTests
{
    private static readonly Guid Cutting = Guid.Parse(input: "22222222-2222-2222-2222-222222222222");
    private static readonly Guid Stopped = Guid.Parse(input: "33333333-3333-3333-3333-333333333333");

    private static readonly DateTime From = new(
        year: 2026, month: 1, day: 1, hour: 10, minute: 0, second: 0, DateTimeKind.Utc);

    private static readonly DateTime To = From.AddHours(value: 1);
    private static readonly DateTime Now = From.AddDays(value: 1);

    private static PhaseIntervalSpan Span(Guid? phaseId, DateTime startedAt, DateTime? endedAt) =>
        new() { PhaseId = phaseId, StartedAt = startedAt, EndedAt = endedAt };

    [Fact]
    public void Calculate_WithIntervalsThatCrossTheRangeBounds_ClipsThemAtTheBounds()
    {
        var intervals = new[]
        {
            Span(Cutting, From.AddMinutes(value: -30), From.AddMinutes(value: 20)),
            Span(Stopped, From.AddMinutes(value: 20), From.AddMinutes(value: 90)),
        };

        var result = PhaseTimeCalculator.Calculate(intervals, From, To, Now);

        result.PhaseSeconds[Cutting].Should().Be(20 * 60);
        result.PhaseSeconds[Stopped].Should().Be(40 * 60);
        result.NoDataSeconds.Should().Be(expected: 0);
    }

    [Fact]
    public void Calculate_WithAnOpenInterval_RunsItToTheEndOfTheRange()
    {
        var intervals = new[] { Span(Cutting, From.AddMinutes(value: 30), endedAt: null) };

        var result = PhaseTimeCalculator.Calculate(intervals, From, To, Now);

        result.PhaseSeconds[Cutting].Should().Be(30 * 60);
        result.NoDataSeconds.Should().Be(30 * 60);
    }

    [Fact]
    public void Calculate_WithAnOpenIntervalAndARangeInTheFuture_RunsItOnlyToNow()
    {
        var intervals = new[] { Span(Cutting, From, endedAt: null) };
        var now = From.AddMinutes(value: 10);

        var result = PhaseTimeCalculator.Calculate(intervals, From, To, now);

        result.PhaseSeconds[Cutting].Should().Be(10 * 60);
        result.NoDataSeconds.Should().Be(50 * 60);
    }

    [Fact]
    public void Calculate_WithUnknownModeAndGaps_AddsUpToTheLengthOfTheRange()
    {
        var intervals = new[]
        {
            Span(Cutting, From, From.AddMinutes(value: 15)),
            Span(phaseId: null, From.AddMinutes(value: 15), From.AddMinutes(value: 25)),
            Span(Stopped, From.AddMinutes(value: 40), From.AddMinutes(value: 50)),
        };

        var result = PhaseTimeCalculator.Calculate(intervals, From, To, Now);

        result.UnknownModeSeconds.Should().Be(10 * 60);
        result.NoDataSeconds.Should().Be(25 * 60);

        (result.PhaseSeconds.Values.Sum() + result.UnknownModeSeconds + result.NoDataSeconds)
            .Should()
            .Be((To - From).TotalSeconds);
    }

    [Fact]
    public void Calculate_WithAnIntervalOutsideTheRange_AddsNothing()
    {
        var intervals = new[] { Span(Cutting, From.AddHours(value: -3), From.AddHours(value: -2)) };

        var result = PhaseTimeCalculator.Calculate(intervals, From, To, Now);

        result.PhaseSeconds.Should().BeEmpty();
        result.NoDataSeconds.Should().Be((To - From).TotalSeconds);
    }
}
