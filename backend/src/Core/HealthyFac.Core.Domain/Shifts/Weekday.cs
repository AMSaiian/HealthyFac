using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Core.Domain.Shifts;

/// <summary>
/// Defines the days of the week of a shift template. The camelCase values reach the API and the database, so never
/// rename one in place.
/// </summary>
public sealed class Weekday : Enumeration<Weekday>
{
    public static readonly Weekday Monday = new(value: "monday", DayOfWeek.Monday);
    public static readonly Weekday Tuesday = new(value: "tuesday", DayOfWeek.Tuesday);
    public static readonly Weekday Wednesday = new(value: "wednesday", DayOfWeek.Wednesday);
    public static readonly Weekday Thursday = new(value: "thursday", DayOfWeek.Thursday);
    public static readonly Weekday Friday = new(value: "friday", DayOfWeek.Friday);
    public static readonly Weekday Saturday = new(value: "saturday", DayOfWeek.Saturday);
    public static readonly Weekday Sunday = new(value: "sunday", DayOfWeek.Sunday);

    private Weekday(string value, DayOfWeek dayOfWeek)
        : base(value)
    {
        DayOfWeek = dayOfWeek;
    }

    public DayOfWeek DayOfWeek { get; }

    /// <summary>
    /// Resolves the weekday of a calendar day of week.
    /// </summary>
    /// <param name="dayOfWeek">The day of week.</param>
    /// <returns>The matching weekday.</returns>
    public static Weekday From(DayOfWeek dayOfWeek) => All.Single(weekday => weekday.DayOfWeek == dayOfWeek);
}
