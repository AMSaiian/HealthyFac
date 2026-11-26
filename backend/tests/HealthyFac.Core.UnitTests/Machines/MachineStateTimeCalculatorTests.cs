using HealthyFac.Core.Application.Machines.States;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Testing.Features;

namespace HealthyFac.Core.UnitTests.Machines;

[Feature(TestFeatures.Machines)]
public sealed class MachineStateTimeCalculatorTests
{
    private static readonly DateTime From = new(
        year: 2026, month: 1, day: 1, hour: 10, minute: 0, second: 0, DateTimeKind.Utc);

    private static readonly DateTime To = From.AddHours(value: 1);

    private static MachineStateSpan Span(MachineState state, DateTime startedAt) =>
        new() { State = state, StartedAt = startedAt };

    [Fact]
    public void Calculate_WithStatesThatCrossTheRangeBounds_ClipsThemAtTheBounds()
    {
        var history = new[]
        {
            Span(MachineState.Operational, From.AddHours(value: -5)),
            Span(MachineState.InMaintenance, From.AddMinutes(value: 20)),
            Span(MachineState.Operational, From.AddMinutes(value: 50)),
        };

        var result = MachineStateTimeCalculator.Calculate(history, From, To);

        result.StateSeconds[MachineState.Operational.Value].Should().Be((20 + 10) * 60);
        result.StateSeconds[MachineState.InMaintenance.Value].Should().Be(30 * 60);
        result.StateSeconds[MachineState.OutOfService.Value].Should().Be(expected: 0);
        result.BeforeCreationSeconds.Should().Be(expected: 0);
    }

    [Fact]
    public void Calculate_WithAMachineCreatedInsideTheRange_CountsTheTimeBeforeTheCreation()
    {
        var history = new[] { Span(MachineState.Operational, From.AddMinutes(value: 15)) };

        var result = MachineStateTimeCalculator.Calculate(history, From, To);

        result.BeforeCreationSeconds.Should().Be(15 * 60);
        result.StateSeconds[MachineState.Operational.Value].Should().Be(45 * 60);
        (result.StateSeconds.Values.Sum() + result.BeforeCreationSeconds).Should().Be((To - From).TotalSeconds);
    }

    [Fact]
    public void Calculate_WithAMachineCreatedAfterTheRange_IsAllBeforeTheCreation()
    {
        var history = new[] { Span(MachineState.Operational, To.AddMinutes(value: 5)) };

        var result = MachineStateTimeCalculator.Calculate(history, From, To);

        result.BeforeCreationSeconds.Should().Be((To - From).TotalSeconds);
        result.StateSeconds.Values.Sum().Should().Be(expected: 0);
    }

    [Fact]
    public void Calculate_WithRowsInAnyOrder_GivesTheSameAnswerAsRowsInOrder()
    {
        var ordered = new[]
        {
            Span(MachineState.Operational, From.AddMinutes(value: -10)),
            Span(MachineState.OutOfService, From.AddMinutes(value: 30)),
        };

        var inOrder = MachineStateTimeCalculator.Calculate(ordered, From, To);
        var reversed = MachineStateTimeCalculator.Calculate(ordered.Reverse(), From, To);

        reversed.StateSeconds.Should().BeEquivalentTo(inOrder.StateSeconds);
    }

    [Fact]
    public void Calculate_WithAStateThatStartsAfterTheRange_ClipsThePreviousStateAtTheEndOfTheRange()
    {
        var history = new[]
        {
            Span(MachineState.Operational, From.AddMinutes(value: -10)),
            Span(MachineState.InMaintenance, To.AddMinutes(value: 5)),
        };

        var result = MachineStateTimeCalculator.Calculate(history, From, To);

        result.StateSeconds[MachineState.Operational.Value].Should().Be((To - From).TotalSeconds);
        result.StateSeconds[MachineState.InMaintenance.Value].Should().Be(expected: 0);
    }
}
