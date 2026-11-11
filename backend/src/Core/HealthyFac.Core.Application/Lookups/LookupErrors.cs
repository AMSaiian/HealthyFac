namespace HealthyFac.Core.Application.Lookups;

/// <summary>Defines the error messages of a lookup.</summary>
public static class LookupErrors
{
    public const string CompanyIdRequired = "CompanyId is required.";

    public static readonly string SearchTooLong =
        $"The search text must have at most {LookupValidationConstants.SearchMaxLength} characters.";
}
