namespace HealthyFac.Core.ApiTests.Shifts.Data;

public static class ShiftApiTestData
{
    public const uint StaleRowVersion = 1;
    public const string WorkerEmail = "worker.shifts@healthyfac.test";
    public const string OtherEmail = "other.shifts@healthyfac.test";
    public const string FirstName = "Wanda";
    public const string LastName = "Worker";
    public const string ReadPermission = "shifts:read";
    public const string ManagePermission = "shifts:manage";
    public const string EmployeesReadPermission = "employees:read";
    public const string Date = "2027-04-12";
    public const string MalformedDate = "not-a-date";
    public const string MalformedId = "not-an-id";
    public const string EmptyBlockList = "[]";
    public const string LatestEndBlock = "16:00-23:55";
    public const string MidnightEndBlock = "16:00-24:00";
    public const int EffectiveFromDaysAhead = 2;
    public const string CompanySchedulePath = "v1/company/schedule";

    public static string TemplatesPath(string employeeId) => $"v1/employees/{employeeId}/shift-templates";

    public static string OverridesPath(string employeeId) =>
        $"v1/employees/{employeeId}/shift-overrides?from={Date}&to={Date}";

    public static string OverridePath(string employeeId) => $"v1/employees/{employeeId}/shift-overrides/{Date}";

    public static string SchedulePath(string employeeId) => $"v1/employees/{employeeId}/schedule?date={Date}";

    public static string ScheduleWithDatePath(string employeeId, string date) =>
        $"v1/employees/{employeeId}/schedule?date={date}";
}
