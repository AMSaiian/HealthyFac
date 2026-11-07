using HealthyFac.Core.Messaging.Employees;

namespace HealthyFac.Core.Application.Employees;

/// <summary>
/// Defines validation length constants for employee and employee invite properties.
/// </summary>
public static class EmployeeValidationConstants
{
    public const int EmailMaxLength = EmployeeMessageLimits.EmailMaxLength;

    public const int NameMaxLength = EmployeeMessageLimits.NameMaxLength;
}
