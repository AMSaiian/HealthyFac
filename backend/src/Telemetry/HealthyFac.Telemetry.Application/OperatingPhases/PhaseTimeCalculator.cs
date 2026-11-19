namespace HealthyFac.Telemetry.Application.OperatingPhases;

/// <summary>
/// One phase interval as the calculator reads it. A <see langword="null"/> end means the interval is open.
/// </summary>
internal readonly record struct PhaseIntervalSpan
{
    /// <summary>The phase, or <see langword="null"/> for the unknown mode.</summary>
    public Guid? PhaseId { get; init; }

    public required DateTime StartedAt { get; init; }

    /// <summary>The end of the interval, or <see langword="null"/> while it is open.</summary>
    public DateTime? EndedAt { get; init; }
}

/// <summary>The seconds of a range, split by phase, the unknown mode, and the time without data.</summary>
internal sealed record PhaseTimes
{
    /// <summary>The seconds in each phase that has time in the range.</summary>
    public required IReadOnlyDictionary<Guid, double> PhaseSeconds { get; init; }

    /// <summary>The seconds in which no phase matched.</summary>
    public required double UnknownModeSeconds { get; init; }

    /// <summary>The rest of the range: the time that no interval covers.</summary>
    public required double NoDataSeconds { get; init; }
}

/// <summary>
/// Clips the phase intervals of a machine to a range and adds up the seconds. The intervals of a machine never
/// overlap, so the phase seconds, the unknown mode seconds, and the no-data seconds add up to the length of the range.
/// </summary>
internal static class PhaseTimeCalculator
{
    /// <summary>
    /// Calculates the time in each phase.
    /// </summary>
    /// <param name="intervals">The intervals of the machine. An interval outside the range adds nothing.</param>
    /// <param name="from">The start of the range.</param>
    /// <param name="to">The end of the range.</param>
    /// <param name="now">The current time: an open interval runs up to it (or up to <paramref name="to"/>, when that is
    /// earlier).</param>
    /// <returns>The seconds of the range.</returns>
    public static PhaseTimes Calculate(
        IEnumerable<PhaseIntervalSpan> intervals,
        DateTime from,
        DateTime to,
        DateTime now
    )
    {
        var phaseSeconds = new Dictionary<Guid, double>();
        var unknownSeconds = 0.0;
        var coveredSeconds = 0.0;

        foreach (var interval in intervals)
        {
            var start = interval.StartedAt > from ? interval.StartedAt : from;
            var rawEnd = interval.EndedAt ?? (now < to ? now : to);
            var end = rawEnd < to ? rawEnd : to;
            var seconds = (end - start).TotalSeconds;

            if (seconds <= 0)
            {
                continue;
            }

            coveredSeconds += seconds;

            if (interval.PhaseId is { } phaseId)
            {
                phaseSeconds[phaseId] = phaseSeconds.GetValueOrDefault(phaseId) + seconds;
            }
            else
            {
                unknownSeconds += seconds;
            }
        }

        return new PhaseTimes
        {
            PhaseSeconds = phaseSeconds,
            UnknownModeSeconds = unknownSeconds,
            NoDataSeconds = Math.Max(val1: 0.0, (to - from).TotalSeconds - coveredSeconds),
        };
    }
}
