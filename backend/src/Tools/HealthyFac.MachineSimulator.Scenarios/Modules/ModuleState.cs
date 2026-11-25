using HealthyFac.MachineSimulator.Scenarios.Curves;

namespace HealthyFac.MachineSimulator.Scenarios.Modules;

/// <summary>
/// The state of one module of one machine: its mode and the progress of the drain from 0 (normal values) to 1 (the
/// trip point).
/// </summary>
/// <param name="kind">The effect of the failure of the module.</param>
public sealed class ModuleState(ModuleKind kind)
{
    public const double NormalLimitProgress = 0.5;
    public const double AlarmProgress = 0.8;
    public const double TripProgress = 1.0;

    private TimeSpan? _drainDuration;

    public ModuleKind Kind { get; } = kind;

    public ModuleMode Mode { get; private set; } = ModuleMode.Expected;

    public double Progress { get; private set; }

    /// <summary>
    /// Changes the mode. <c>expected</c> clears the progress and a failure. <c>draining</c> starts at 0 from
    /// <c>expected</c> or <c>failure</c>, and a repeat changes nothing. <c>failure</c> applies at once, and a repeat
    /// changes nothing. A new <c>expected</c> also clears the drain duration of the module.
    /// </summary>
    /// <param name="mode">The new mode.</param>
    /// <param name="drainDuration">
    /// The run time that a full drain of this module needs, stored when <c>draining</c> starts. <see langword="null"/>
    /// keeps the duration that <see cref="Advance"/> gets.
    /// </param>
    public void SetMode(ModuleMode mode, TimeSpan? drainDuration = null)
    {
        if (mode == ModuleMode.Expected)
        {
            Mode = ModuleMode.Expected;
            Progress = 0;
            _drainDuration = null;
        }
        else if (mode == ModuleMode.Draining && Mode != ModuleMode.Draining)
        {
            Mode = ModuleMode.Draining;
            Progress = 0;
            _drainDuration = drainDuration;
        }
        else if (mode == ModuleMode.Failure && Mode != ModuleMode.Failure)
        {
            Mode = ModuleMode.Failure;
            Progress = TripProgress;
        }
    }

    /// <summary>
    /// Moves a draining module forward. The module reaches the failure by itself when the progress reaches 1.
    /// </summary>
    /// <param name="runTime">The run time since the last call.</param>
    /// <param name="drainDuration">
    /// The run time that a full drain needs, unless <see cref="SetMode"/> stored a duration for this drain.
    /// </param>
    /// <returns>The milestones that this call crossed, each once per drain.</returns>
    public IReadOnlyList<DrainMilestone> Advance(TimeSpan runTime, TimeSpan drainDuration)
    {
        if (Mode != ModuleMode.Draining)
        {
            return [];
        }

        var before = Progress;
        Progress = Math.Min(TripProgress, before + runTime / (_drainDuration ?? drainDuration));

        List<DrainMilestone> crossed = [];
        AddIfCrossed(crossed, before, NormalLimitProgress, DrainMilestone.NormalLimit);
        AddIfCrossed(crossed, before, AlarmProgress, DrainMilestone.Alarm);
        AddIfCrossed(crossed, before, TripProgress, DrainMilestone.Trip);

        if (Progress >= TripProgress)
        {
            Mode = ModuleMode.Failure;
        }

        return crossed;
    }

    private void AddIfCrossed(List<DrainMilestone> crossed, double before, double threshold, DrainMilestone milestone)
    {
        if (before < threshold && Progress >= threshold)
        {
            crossed.Add(milestone);
        }
    }
}
