namespace HealthyFac.Shared.Application.Grids;

/// <summary>
/// Defines the limits of a grid request. Every grid shares them, and the frontend page sizes stay below the maximum.
/// </summary>
public static class GridValidationConstants
{
    public const int MinLimit = 1;
    public const int MaxLimit = 100;
    public const int MinOffset = 0;
}
