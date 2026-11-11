namespace HealthyFac.Core.Application.Shifts;

/// <summary>
/// Defines the input limits of shifts. The step of a block time is <c>SlotRules.GridStep</c> in the Domain layer.
/// </summary>
public static class ShiftValidationConstants
{
    public const int MaxBlocksPerDay = 6;

    /// <summary>How many days ahead a version or an override may start.</summary>
    public const int MaxDaysAhead = 365;

    public const int WeekdayCount = 7;

    /// <summary>The most work orders that a rejected shift change lists.</summary>
    public const int MaxAffectedWorkOrders = 50;

    /// <summary>The longest date range of an override read, in days.</summary>
    public const int MaxOverrideRangeDays = 62;
}
