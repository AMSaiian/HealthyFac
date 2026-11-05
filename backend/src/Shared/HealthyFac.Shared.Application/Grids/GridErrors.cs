namespace HealthyFac.Shared.Application.Grids;

/// <summary>
/// Defines the validation messages and the exceptions of the grid contract.
/// </summary>
public static class GridErrors
{
    public static class Validation
    {
        public static readonly string OffsetOutOfRange =
            $"Offset must be at least {GridValidationConstants.MinOffset}.";

        public static readonly string LimitOutOfRange =
            $"Limit must be between {GridValidationConstants.MinLimit} and {GridValidationConstants.MaxLimit}.";

        public const string SortColumnUnknown = "Sort column is not sortable.";

        public const string FilterRequired = "Filter is required.";

        public const string DateRangeFromNotBeforeTo = "The start of the date range must be before its end.";

        public const string DateRangeNotUtc = "A date range bound must be a UTC instant (end it with Z).";

        public const string TextFilterValueRequired = "A text filter needs a value.";

        public const string SetFilterValuesRequired = "A set filter needs its values.";

        public const string IdSetFilterIdsRequired = "An ID set filter needs its IDs.";

        public const string IdSetFilterIdEmpty = "An ID set filter must not hold an empty ID.";
    }

    public static InvalidOperationException SortColumnNotMapped(object column) =>
        new($"The sort column '{column}' has no entry in the sort map. Each sort column maps to one key expression.");
}
