namespace HealthyFac.Core.Application.Employees;

/// <summary>
/// Defines validation length constants for employee and employee invite properties.
/// </summary>
public static class EmployeeValidationConstants
{
    /// <summary>The longest email address that SMTP allows (RFC 5321).</summary>
    public const int EmailMaxLength = 254;

    public const int NameMaxLength = 100;
}
