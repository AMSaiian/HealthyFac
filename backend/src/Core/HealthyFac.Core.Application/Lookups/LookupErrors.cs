using HealthyFac.Shared.Application.Grids;

namespace HealthyFac.Core.Application.Lookups;

/// <summary>Defines the error messages of a lookup.</summary>
public static class LookupErrors
{
    public const string CompanyIdRequired = "CompanyId is required.";

    public static readonly string TooManyIds =
        $"The lookup takes at most {GridValidationConstants.MaxLimit} IDs.";

    public static readonly string SearchTooLong =
        $"The search text must have at most {LookupValidationConstants.SearchMaxLength} characters.";
}
