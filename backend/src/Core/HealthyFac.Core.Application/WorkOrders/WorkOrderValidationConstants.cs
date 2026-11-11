namespace HealthyFac.Core.Application.WorkOrders;

/// <summary>
/// Defines the input limits of a work order.
/// </summary>
public static class WorkOrderValidationConstants
{
    public const int TitleMaxLength = 200;

    /// <summary>The limit counts the visible characters of the description, not the stored HTML.</summary>
    public const int DescriptionMaxLength = 5000;

    public const int FileMaxCount = 20;

    /// <summary>The searches of one batch: one for each status column of the board.</summary>
    public const int BatchMaxSearches = 6;

    public const int EstimateMinMinutes = 5;

    public const int EstimateMaxMinutes = 720;

    /// <summary>The estimate is a multiple of this step, like the planned start.</summary>
    public const int EstimateStepMinutes = 5;
}
