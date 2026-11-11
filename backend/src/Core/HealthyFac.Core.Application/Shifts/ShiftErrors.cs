using HealthyFac.Core.Domain.WorkOrders.Scheduling;

namespace HealthyFac.Core.Application.Shifts;

/// <summary>
/// Defines error messages for shift operations, grouped by the kind of failure.
/// </summary>
public static class ShiftErrors
{
    public static class Validation
    {
        public const string CompanyIdRequired = "CompanyId is required.";
        public const string EmployeeIdRequired = "EmployeeId is required.";
        public const string VersionIdRequired = "VersionId is required.";

        public const string LocalTimeDoesNotExist =
            "The local time does not exist on that date (a clock change skips it).";

        public const string BlockEndBeforeStart = "Each block must end after it starts.";
        public const string BlocksUnsorted = "The blocks must be sorted by their start.";
        public const string BlocksOverlap = "The blocks must not overlap.";
        public const string DaysInvalid = "The template needs exactly one entry for each weekday.";
        public const string BlocksRequired = "The blocks of a day are required.";
        public const string WeekdayRequired = "The weekday of a day is required.";

        public const string RangeInvalid = "The end of the range must not be before its start.";

        public static readonly string RangeTooLong =
            $"The range has at most {ShiftValidationConstants.MaxOverrideRangeDays} days.";

        public static readonly string TooManyBlocks =
            $"A day has at most {ShiftValidationConstants.MaxBlocksPerDay} blocks.";

        public static readonly string BlockOffGrid =
            $"Block times must be a multiple of {SlotRules.GridStep.TotalMinutes} minutes.";

        public static readonly string EffectiveFromOutOfRange =
            $"The date must be from today up to {ShiftValidationConstants.MaxDaysAhead} days ahead.";

        public static readonly string DateOutOfRange = EffectiveFromOutOfRange;
    }

    public static class Rejections
    {
        public const string EmployeeNotFound = "The employee does not exist in the company.";
        public const string VersionNotFound = "The shift template version does not exist.";
        public const string VersionInEffect = "A version that already took effect cannot change.";
        public const string PastBlockChanged = "The part of today that already passed cannot change.";
        public const string WorkOrdersOutsideShift = "The change leaves scheduled work orders outside the shift.";
        public const string VersionChanged = "The version changed at the same time. Reload it and try again.";
        public const string EffectiveFromTaken = "The employee already has a version for this date.";
        public const string OverrideNotFound = "The employee has no override for this date.";
        public const string OverrideChanged = "The override changed at the same time. Reload it and try again.";
    }
}
