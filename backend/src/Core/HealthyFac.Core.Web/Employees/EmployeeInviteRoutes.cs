namespace HealthyFac.Core.Web.Employees;

/// <summary>
/// Defines route constants for employee invite endpoints.
/// </summary>
public static class EmployeeInviteRoutes
{
    public const string BaseRoute = "/employee-invites";
    public const string IdParameter = "id";
    public const string RowVersionParameter = "rowVersion";
    public const string ById = "/{" + IdParameter + "}";
    public const string Root = "/";
    public const string Resend = "/resend";
    public const string Verify = "/verify";
    public const string Accept = "/accept";
    public const string Search = "/search";
    public const string ResourceLocationFormat = "{0}/{1}";
}
