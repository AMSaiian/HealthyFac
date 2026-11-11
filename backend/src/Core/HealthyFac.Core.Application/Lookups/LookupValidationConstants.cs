namespace HealthyFac.Core.Application.Lookups;

/// <summary>Defines the limits of a lookup.</summary>
public static class LookupValidationConstants
{
    /// <summary>The most options that a lookup answers. A search narrows the list; there is no second page.</summary>
    public const int MaxItems = 50;

    public const int SearchMaxLength = 100;
}
