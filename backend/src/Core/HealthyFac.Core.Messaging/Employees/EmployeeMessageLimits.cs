namespace HealthyFac.Core.Messaging.Employees;

/// <summary>
/// Defines the limits of the employee data that the Core messages carry. The message validators and the Core
/// Application limits (<c>EmployeeValidationConstants</c>) use the same values.
/// </summary>
public static class EmployeeMessageLimits
{
    /// <summary>The longest email address that SMTP allows (RFC 5321).</summary>
    public const int EmailMaxLength = 254;

    public const int NameMaxLength = 100;
}
