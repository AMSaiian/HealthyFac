namespace HealthyFac.Core.Domain.Shifts.DaySchedules;

/// <summary>
/// A time span of a work block that no scheduled work order occupies. Its bounds are on the 5-minute grid.
/// </summary>
public sealed record FreeSpan
{
    public required DateTime StartUtc { get; init; }

    public required DateTime EndUtc { get; init; }

    /// <summary>The start as a time in the company zone, with <see cref="DateTimeKind.Unspecified"/>.</summary>
    public required DateTime StartLocal { get; init; }

    /// <summary>The end as a time in the company zone, with <see cref="DateTimeKind.Unspecified"/>.</summary>
    public required DateTime EndLocal { get; init; }
}
