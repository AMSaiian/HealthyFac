namespace HealthyFac.Telemetry.Domain.Evaluation.Phases;

/// <summary>
/// What one sample did to the phase tracking.
/// </summary>
public readonly record struct PhaseStep
{
    /// <summary>The statistics may learn from this sample.</summary>
    public required bool IsLearningOpen { get; init; }

    /// <summary>The current phase changed at this sample.</summary>
    public required bool IsSwitched { get; init; }
}

/// <summary>
/// Follows the current phase of a machine. A short change of the matched phase does not switch: the match must
/// differ from the current phase for the dwell time. The statistics never learn while the phase is uncertain.
/// The evaluation does not use it for a machine without active phases: that machine always has the phase none.
/// </summary>
public class PhaseTracking
{
    public Guid? CurrentPhaseId { get; private set; }

    public DateTime? SwitchedAt { get; private set; }

    /// <summary>The time since which the match differs from the current phase.</summary>
    public DateTime? MismatchSince { get; private set; }

    /// <summary>The time since which no phase matches.</summary>
    public DateTime? UnmatchedSince { get; private set; }

    /// <summary>
    /// Follows one sample.
    /// </summary>
    /// <param name="matched">The phase that the sample matches, or <see langword="null"/>.</param>
    /// <param name="t">The <c>sampled_at</c> time of the sample.</param>
    /// <param name="settings">The values of the detection options.</param>
    /// <returns>Whether the learning gate is open (it is closed during a mismatch, within the settle time after a
    /// switch, and while the current phase is <see langword="null"/>) and whether the phase switched.</returns>
    public PhaseStep Advance(Guid? matched, DateTime t, PhaseSettings settings)
    {
        UnmatchedSince = matched is null ? UnmatchedSince ?? t : null;
        var isSwitched = false;

        if (matched == CurrentPhaseId)
        {
            MismatchSince = null;
        }
        else
        {
            MismatchSince ??= t;

            if ((t - MismatchSince.Value).TotalSeconds >= settings.PhaseDwellSeconds)
            {
                CurrentPhaseId = matched;
                SwitchedAt = t;
                MismatchSince = null;
                isSwitched = true;
            }
        }

        var isSettling = SwitchedAt is { } switchedAt && (t - switchedAt).TotalSeconds < settings.SettleTimeSeconds;
        var isLearningOpen = MismatchSince is null && !isSettling && CurrentPhaseId is not null;

        return new PhaseStep { IsLearningOpen = isLearningOpen, IsSwitched = isSwitched };
    }
}
