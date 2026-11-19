using HealthyFac.Core.Domain.Machines;

namespace HealthyFac.Core.Application.Machines.States;

/// <summary>One row of the state history: the state and the moment it started.</summary>
public readonly record struct MachineStateSpan
{
    public required MachineState State { get; init; }

    /// <summary>The start of the state. It lasts until the start of the next row.</summary>
    public required DateTime StartedAt { get; init; }
}

/// <summary>The seconds of a range, split by machine state, plus the time before the machine existed.</summary>
public sealed record MachineStateTimes
{
    /// <summary>The seconds in each state, by the value of the state.</summary>
    public required IReadOnlyDictionary<string, double> StateSeconds { get; init; }

    /// <summary>The seconds of the range before the first state row.</summary>
    public required double BeforeCreationSeconds { get; init; }
}

/// <summary>
/// Clips the state history of a machine to a range and adds up the seconds. Each row lasts until the start of the next
/// one, and the last row lasts to the end of the range. The seconds of all states and the seconds before the creation
/// add up to the length of the range.
/// </summary>
public static class MachineStateTimeCalculator
{
    /// <summary>
    /// Calculates the time in each state.
    /// </summary>
    /// <param name="history">The state rows of the machine, in any order.</param>
    /// <param name="from">The start of the range.</param>
    /// <param name="to">The end of the range.</param>
    /// <returns>
    /// The seconds of the range. Every state of <see cref="MachineState"/> has an entry, also with 0 seconds.
    /// </returns>
    public static MachineStateTimes Calculate(IEnumerable<MachineStateSpan> history, DateTime from, DateTime to)
    {
        var rows = history.OrderBy(row => row.StartedAt).ToList();
        var stateSeconds = MachineState.All.ToDictionary(state => state.Value, _ => 0.0);

        for (var index = 0; index < rows.Count; index++)
        {
            var start = rows[index].StartedAt > from ? rows[index].StartedAt : from;
            var rawEnd = index + 1 < rows.Count ? rows[index + 1].StartedAt : to;
            var end = rawEnd < to ? rawEnd : to;

            stateSeconds[rows[index].State.Value] += Math.Max(val1: 0.0, (end - start).TotalSeconds);
        }

        var firstStart = rows.Count == 0 ? to : rows[index: 0].StartedAt;
        var beforeEnd = firstStart < to ? firstStart : to;

        return new MachineStateTimes
        {
            StateSeconds = stateSeconds,
            BeforeCreationSeconds = Math.Max(val1: 0.0, (beforeEnd - from).TotalSeconds),
        };
    }
}
