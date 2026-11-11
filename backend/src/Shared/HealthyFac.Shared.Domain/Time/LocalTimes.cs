namespace HealthyFac.Shared.Domain.Time;

/// <summary>
/// Converts a planned local time to a UTC instant with the <c>compatible</c> rule of Temporal, so the backend and
/// the frontend compute the same instant for a time in a skipped or repeated hour.
/// </summary>
public static class LocalTimes
{
    /// <summary>
    /// Converts a local time of a zone to UTC. A local time in a skipped hour (the gap of a forward change) uses the
    /// offset before the gap, so it moves forward by the gap length. A local time in a repeated hour uses the offset
    /// before the change, so it is the earlier instant.
    /// </summary>
    /// <param name="local">The local time. Its <see cref="DateTime.Kind"/> is ignored.</param>
    /// <param name="zone">The zone of the local time.</param>
    /// <returns>The UTC time with <see cref="DateTimeKind.Utc"/>.</returns>
    public static DateTime ToUtc(DateTime local, TimeZoneInfo zone)
    {
        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        var offset = zone.IsInvalidTime(unspecified)
            ? zone.GetUtcOffset(unspecified.AddDays(value: -1))
            : zone.IsAmbiguousTime(unspecified)
                ? zone.GetAmbiguousTimeOffsets(unspecified).Max()
                : zone.GetUtcOffset(unspecified);

        return DateTime.SpecifyKind(unspecified - offset, DateTimeKind.Utc);
    }

    /// <summary>
    /// Tells whether a local time does not exist in a zone because a forward change skips it.
    /// </summary>
    /// <param name="local">The local time. Its <see cref="DateTime.Kind"/> is ignored.</param>
    /// <param name="zone">The zone of the local time.</param>
    /// <returns><see langword="true"/> when the local time is in a skipped hour.</returns>
    public static bool IsSkipped(DateTime local, TimeZoneInfo zone)
        => zone.IsInvalidTime(DateTime.SpecifyKind(local, DateTimeKind.Unspecified));
}
