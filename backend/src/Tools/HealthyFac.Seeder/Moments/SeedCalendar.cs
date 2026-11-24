namespace HealthyFac.Seeder.Moments;

/// <summary>
/// Turns relative moments into UTC instants and dates. The seed day is the local date of the anchor in the zone.
/// </summary>
public static class SeedCalendar
{
    /// <summary>Resolves a moment that has a time to a UTC instant.</summary>
    /// <param name="moment">The relative moment.</param>
    /// <param name="anchor">The anchor: the start of the job.</param>
    /// <param name="zone">The company zone.</param>
    /// <returns>The UTC instant.</returns>
    /// <exception cref="MomentException">
    /// The moment has no time, or its local time does not exist or is ambiguous on its day.
    /// </exception>
    public static DateTimeOffset ToUtc(RelativeMoment moment, DateTimeOffset anchor, TimeZoneInfo zone)
    {
        if (moment.Form == MomentForm.Hour)
        {
            return anchor.AddHours(moment.Offset).ToUniversalTime();
        }

        if (moment.Time is not { } time)
        {
            throw new MomentException(MomentErrors.TimeRequired(moment.ToString()));
        }

        var date = ToDate(moment, anchor, zone);
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        if (zone.IsInvalidTime(local))
        {
            throw new MomentException(MomentErrors.TimeDoesNotExist(moment.ToString(), date));
        }

        if (zone.IsAmbiguousTime(local))
        {
            throw new MomentException(MomentErrors.TimeIsAmbiguous(moment.ToString(), date));
        }

        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, zone), TimeSpan.Zero);
    }

    /// <summary>Resolves a moment that has a calendar day to its local date.</summary>
    /// <param name="moment">The relative moment.</param>
    /// <param name="anchor">The anchor: the start of the job.</param>
    /// <param name="zone">The company zone.</param>
    /// <returns>The local date.</returns>
    /// <exception cref="MomentException">The moment is in hours and has no calendar date.</exception>
    public static DateOnly ToDate(RelativeMoment moment, DateTimeOffset anchor, TimeZoneInfo zone)
    {
        var seedDay = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(anchor, zone).DateTime);

        return moment.Form switch
        {
            MomentForm.Workday => AddWorkdays(seedDay, moment.Offset),
            MomentForm.Day or MomentForm.Date => seedDay.AddDays(moment.Offset),
            _ => throw new MomentException(MomentErrors.DateRequired(moment.ToString())),
        };
    }

    private static DateOnly AddWorkdays(DateOnly seedDay, int count)
    {
        var current = seedDay;
        while (!IsWorkday(current))
        {
            current = current.AddDays(value: 1);
        }

        var step = Math.Sign(count);
        for (var left = Math.Abs(count); left > 0; left--)
        {
            do
            {
                current = current.AddDays(step);
            }
            while (!IsWorkday(current));
        }

        return current;
    }

    private static bool IsWorkday(DateOnly date) => date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
}
