namespace HealthyFac.Core.Web.Shifts;

/// <summary>
/// Defines route constants for shift endpoints.
/// </summary>
public static class ShiftRoutes
{
    public const string EmployeeIdParameter = "id";
    public const string VersionIdParameter = "versionId";
    public const string DateParameter = "date";
    public const string RowVersionParameter = "rowVersion";

    public const string EmployeeRoute = "/employees/{" + EmployeeIdParameter + "}";
    public const string Templates = "/shift-templates";
    public const string TemplateById = Templates + "/{" + VersionIdParameter + "}";
    public const string Overrides = "/shift-overrides";
    public const string OverrideByDate = Overrides + "/{" + DateParameter + "}";
    public const string Schedule = "/schedule";
    public const string ResourceLocationFormat = "{0}/{1}";
}
