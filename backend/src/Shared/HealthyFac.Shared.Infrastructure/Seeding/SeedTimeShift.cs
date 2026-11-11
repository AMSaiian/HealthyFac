namespace HealthyFac.Shared.Infrastructure.Seeding;

/// <summary>
/// Moves the business timestamps of the demo ingest snapshot by one offset, so the newest snapshot event happened
/// <see cref="NewestAge"/> before the seed time and every gap between two timestamps stays as captured.
/// </summary>
/// <remarks>
/// Each service reads the same <c>referenceTime</c> from its file and the same clock, so each computes the same offset
/// on its own.
/// </remarks>
public sealed class SeedTimeShift
{
    /// <summary>How long before the seed time the newest snapshot event is placed.</summary>
    public static readonly TimeSpan NewestAge = TimeSpan.FromHours(1);

    private SeedTimeShift(TimeSpan offset)
    {
        Offset = offset;
    }

    /// <summary>
    /// The value that <see cref="Shift"/> adds: the seed time minus <see cref="NewestAge"/> minus the reference time.
    /// </summary>
    public TimeSpan Offset { get; }

    /// <summary>
    /// Builds the shift for a seed run.
    /// </summary>
    /// <param name="referenceTime">The newest finish time of the snapshot (<c>referenceTime</c> of the files).</param>
    /// <param name="timeProvider">Provides the seed time.</param>
    /// <returns>The shift.</returns>
    public static SeedTimeShift Create(DateTimeOffset referenceTime, TimeProvider timeProvider) =>
        new(timeProvider.GetUtcNow() - NewestAge - referenceTime);

    /// <summary>
    /// Shifts a snapshot timestamp. A value without a kind counts as UTC.
    /// </summary>
    /// <param name="value">The snapshot value, or <see langword="null"/>.</param>
    /// <returns>The shifted UTC value, or <see langword="null"/> for a <see langword="null"/> value.</returns>
    public DateTime? Shift(DateTime? value) =>
        value is null
            ? null
            : DateTime.SpecifyKind(
                value.Value.Kind == DateTimeKind.Local ? value.Value.ToUniversalTime() : value.Value,
                DateTimeKind.Utc) + Offset;
}
