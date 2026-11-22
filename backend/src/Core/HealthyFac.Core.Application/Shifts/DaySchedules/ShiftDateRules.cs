using FluentValidation.Results;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.Application.Shifts.DaySchedules;

/// <summary>
/// The date rules of an override write: a date is not in the past, and not further ahead than the allowed days.
/// </summary>
public static class ShiftDateRules
{
    private const string DateKey = "date";

    /// <summary>
    /// Rejects a date before today.
    /// </summary>
    /// <param name="date">The local date of the override.</param>
    /// <param name="today">Today in the company zone.</param>
    /// <exception cref="AppValidationException">The date is before <paramref name="today"/>.</exception>
    public static void EnsureNotPast(DateOnly date, DateOnly today)
    {
        if (date < today)
        {
            throw DateOutOfRange();
        }
    }

    /// <summary>
    /// Rejects a date before today or later than <see cref="ShiftValidationConstants.MaxDaysAhead"/> days ahead.
    /// </summary>
    /// <param name="date">The local date of the override.</param>
    /// <param name="today">Today in the company zone.</param>
    /// <exception cref="AppValidationException">The date is out of the range.</exception>
    public static void EnsureInRange(DateOnly date, DateOnly today)
    {
        EnsureNotPast(date, today);
        if (date > today.AddDays(ShiftValidationConstants.MaxDaysAhead))
        {
            throw DateOutOfRange();
        }
    }

    private static AppValidationException DateOutOfRange() =>
        new([new ValidationFailure(DateKey, ShiftErrors.Validation.DateOutOfRange)]);
}
