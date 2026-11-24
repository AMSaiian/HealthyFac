using HealthyFac.MachineSimulator.Scenarios.Modules;

namespace HealthyFac.MachineSimulator.Scenarios.Curves;

/// <summary>
/// The value of one metric along a drain. A curve of 4 points (reference, normal limit, alarm, trip) sits at the
/// progress 0, 0.5, 0.8, and 1.0. A curve of 2 points (start, end) is linear over 0 to 1. The value is linear between
/// two points.
/// </summary>
public sealed class DrainCurve
{
    private readonly (double Progress, double Value)[] _points;

    private DrainCurve((double Progress, double Value)[] points)
    {
        _points = points;
    }

    /// <summary>
    /// Creates a curve of 4 points.
    /// </summary>
    /// <param name="reference">The value without a drain (progress 0).</param>
    /// <param name="normalLimit">The value at the normal limit (progress 0.5).</param>
    /// <param name="alarm">The value at the alarm (progress 0.8).</param>
    /// <param name="trip">The value at the trip (progress 1.0).</param>
    /// <returns>The curve.</returns>
    public static DrainCurve FourPoints(double reference, double normalLimit, double alarm, double trip) =>
        new(
        [
            (0, reference),
            (ModuleState.NormalLimitProgress, normalLimit),
            (ModuleState.AlarmProgress, alarm),
            (ModuleState.TripProgress, trip),
        ]);

    /// <summary>
    /// Creates a linear curve of 2 points.
    /// </summary>
    /// <param name="start">The value at progress 0.</param>
    /// <param name="end">The value at progress 1.</param>
    /// <returns>The curve.</returns>
    public static DrainCurve TwoPoints(double start, double end) => new([(0, start), (ModuleState.TripProgress, end)]);

    /// <summary>The value without a drain.</summary>
    public double Reference => _points[0].Value;

    /// <summary>The value at the end of the drain.</summary>
    public double Trip => _points[^1].Value;

    /// <summary>
    /// Reads the value at a progress.
    /// </summary>
    /// <param name="progress">The progress from 0 to 1. A value outside the range is limited to it.</param>
    /// <returns>The value of the curve.</returns>
    public double ValueAt(double progress)
    {
        var clamped = Math.Clamp(progress, min: 0, ModuleState.TripProgress);

        for (var index = 1; index < _points.Length; index++)
        {
            var (endProgress, endValue) = _points[index];
            if (clamped <= endProgress)
            {
                var (startProgress, startValue) = _points[index - 1];

                return startValue + (endValue - startValue) * (clamped - startProgress) / (endProgress - startProgress);
            }
        }

        return _points[^1].Value;
    }

    /// <summary>
    /// Reads the offset of the curve at a progress: the value minus the reference value.
    /// </summary>
    /// <param name="progress">The progress from 0 to 1.</param>
    /// <returns>The drain offset.</returns>
    public double OffsetAt(double progress) => ValueAt(progress) - Reference;
}
