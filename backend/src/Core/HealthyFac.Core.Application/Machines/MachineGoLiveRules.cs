using FluentValidation.Results;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Domain.Time;

namespace HealthyFac.Core.Application.Machines;

/// <summary>
/// Checks the expected go-live day of a machine against the days of the company time zone.
/// </summary>
public static class MachineGoLiveRules
{
    private static readonly string ExpectedGoLiveOnKey = ValidationNames.GetJsonName(
        typeof(CreateMachineRequest).GetProperty(nameof(CreateMachineRequest.ExpectedGoLiveOn))!);

    /// <summary>
    /// Converts a UTC instant to the day in a company time zone.
    /// </summary>
    /// <param name="instantUtc">The UTC instant.</param>
    /// <param name="zone">The time zone of the company.</param>
    /// <returns>The company day of the instant.</returns>
    public static DateOnly DayOf(DateTime instantUtc, TimeZoneInfo zone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(instantUtc, zone));

    /// <summary>
    /// Converts the start of a company day to a UTC instant. A start in a skipped hour moves forward by the gap
    /// length, and a start in a repeated hour is the earlier instant (<see cref="LocalTimes.ToUtc"/>).
    /// </summary>
    /// <param name="day">The day in the company zone.</param>
    /// <param name="zone">The time zone of the company.</param>
    /// <returns>The UTC instant of the local midnight of <paramref name="day"/>.</returns>
    public static DateTime StartOfDayUtc(DateOnly day, TimeZoneInfo zone) =>
        LocalTimes.ToUtc(day.ToDateTime(TimeOnly.MinValue), zone);

    /// <summary>
    /// Requires a day that is today or later in the company zone.
    /// </summary>
    /// <param name="expectedGoLiveOn">The day to check.</param>
    /// <param name="nowUtc">The current UTC instant.</param>
    /// <param name="zone">The time zone of the company.</param>
    /// <exception cref="AppValidationException">The day is before today in the company zone.</exception>
    public static void EnsureNotBeforeToday(DateOnly expectedGoLiveOn, DateTime nowUtc, TimeZoneInfo zone)
    {
        if (expectedGoLiveOn < DayOf(nowUtc, zone))
        {
            throw new AppValidationException(
                [new ValidationFailure(ExpectedGoLiveOnKey, MachineErrors.ExpectedGoLiveOnBeforeToday)]);
        }
    }

    /// <summary>
    /// Requires that an update draft carries no go-live day, because it keeps the stored day.
    /// </summary>
    /// <param name="expectedGoLiveOn">The day of the apply request, or <see langword="null"/>.</param>
    /// <exception cref="AppValidationException">The request carries a day.</exception>
    public static void EnsureNoDateOnUpdateDraft(DateOnly? expectedGoLiveOn)
    {
        if (expectedGoLiveOn is not null)
        {
            throw new AppValidationException(
                [new ValidationFailure(ExpectedGoLiveOnKey, MachineErrors.ExpectedGoLiveOnOnUpdateDraft)]);
        }
    }

    /// <summary>
    /// Requires a day that is not before the creation day of the machine in the company zone.
    /// </summary>
    /// <param name="expectedGoLiveOn">The day to check.</param>
    /// <param name="createdAtUtc">The UTC instant that the machine was created.</param>
    /// <param name="zone">The time zone of the company.</param>
    /// <exception cref="AppValidationException">The day is before the creation day of the machine.</exception>
    public static void EnsureNotBeforeCreation(DateOnly expectedGoLiveOn, DateTime createdAtUtc, TimeZoneInfo zone)
    {
        if (expectedGoLiveOn < DayOf(createdAtUtc, zone))
        {
            throw new AppValidationException(
                [new ValidationFailure(ExpectedGoLiveOnKey, MachineErrors.ExpectedGoLiveOnBeforeCreation)]);
        }
    }
}
