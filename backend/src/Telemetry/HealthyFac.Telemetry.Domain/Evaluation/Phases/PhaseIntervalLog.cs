namespace HealthyFac.Telemetry.Domain.Evaluation.Phases;

/// <summary>
/// Follows the phase intervals of one machine during a run. It keeps the open interval (at most 1), closes it when the
/// current phase switches or when the data stops, and opens the next one at the sample that follows. A machine
/// without phases never calls it, so it writes no interval.
/// </summary>
public sealed class PhaseIntervalLog
{
    private readonly Guid _machineRegistrationId;

    /// <summary>
    /// Creates the log of a machine.
    /// </summary>
    /// <param name="machineRegistrationId">The registration of the machine.</param>
    /// <param name="open">The stored open interval of the machine, or <see langword="null"/>.</param>
    public PhaseIntervalLog(Guid machineRegistrationId, PhaseInterval? open)
    {
        _machineRegistrationId = machineRegistrationId;
        Open = open;
    }

    /// <summary>The open interval, or <see langword="null"/> while the machine has no data in a phase.</summary>
    public PhaseInterval? Open { get; private set; }

    /// <summary>
    /// Follows one sample. The first sample after a gap opens an interval. A switch of the current phase closes the
    /// open interval and opens the next one at the sample time.
    /// </summary>
    /// <param name="phaseId">
    /// The current phase after the sample, or <see langword="null"/> for the unknown mode.
    /// </param>
    /// <param name="isSwitched">Tells whether the current phase changed at this sample.</param>
    /// <param name="sampledAt">The <c>sampled_at</c> time of the sample.</param>
    /// <returns>The new interval that the caller stores, or <see langword="null"/> when none opened.</returns>
    public PhaseInterval? Follow(Guid? phaseId, bool isSwitched, DateTime sampledAt)
    {
        if (Open is not null && !isSwitched)
        {
            return null;
        }

        Open?.Close(sampledAt);
        Open = new PhaseInterval(_machineRegistrationId, phaseId, sampledAt);

        return Open;
    }

    /// <summary>
    /// Closes the open interval, because the machine has no data from this time on (the run skips the machine, or the
    /// silence rule opens).
    /// </summary>
    /// <param name="endedAt">The time of the last sample that the interval covers.</param>
    public void Close(DateTime endedAt)
    {
        Open?.Close(endedAt);
        Open = null;
    }
}
