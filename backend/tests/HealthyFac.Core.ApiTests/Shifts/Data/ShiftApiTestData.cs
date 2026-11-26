using System.Text.Json.Nodes;

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
    public const string EarlierDate = "2027-04-11";
    public const string MalformedDate = "not-a-date";
    public const string MalformedId = "not-an-id";
    public const string EmptyBlockList = "[]";
    public const string LatestEndBlock = "16:00-23:55";
    public const string MidnightEndBlock = "16:00-24:00";
    public const int EffectiveFromDaysAhead = 2;

    public static readonly IReadOnlyList<string> SevenBlocks =
        ["01:00-02:00", "03:00-04:00", "05:00-06:00", "07:00-08:00", "09:00-10:00", "11:00-12:00", "13:00-14:00"];

    public const string CompanySchedulePath = "v1/company/schedule";

    public static string VersionBody(IReadOnlyList<string> blockRanges, int daysAhead)
    {
        var effectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(daysAhead);
        var days = new JsonArray();

        foreach (var weekday in new[] { "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday" })
        {
            days.Add(
                new JsonObject
                {
                    ["weekday"] = weekday,
                    ["blocks"] = new JsonArray(blockRanges.Select(ToBlock).ToArray()),
                });
        }

        return new JsonObject
        {
            ["effectiveFrom"] = effectiveFrom.ToString(format: "yyyy-MM-dd"),
            ["days"] = days,
        }.ToJsonString();
    }

    public static string TemplatesPath(string employeeId) => $"v1/employees/{employeeId}/shift-templates";

    public static string OverridesPath(string employeeId) =>
        $"v1/employees/{employeeId}/shift-overrides?from={Date}&to={Date}";

    public static string OverridePath(string employeeId) => $"v1/employees/{employeeId}/shift-overrides/{Date}";

    public static string ScheduleDaysPath(string employeeId, string from, string to) =>
        $"v1/employees/{employeeId}/schedule/days?from={from}&to={to}";

    public static string FreeSpansPath(string employeeId, string date) =>
        $"v1/employees/{employeeId}/schedule/free-spans?date={date}";

    public static string SchedulePath(string employeeId) => $"v1/employees/{employeeId}/schedule?date={Date}";

    public static string ScheduleWithDatePath(string employeeId, string date) =>
        $"v1/employees/{employeeId}/schedule?date={date}";

    public static TheoryData<string, string, string> WorkerWriteRoutes => new()
    {
        { "CreateTemplate", "POST", TemplatesPath(employeeId: "{employeeId}") },
        { "SetOverride", "PUT", OverridePath(employeeId: "{employeeId}") },
        { "DeleteOverride", "DELETE", OverridePath(employeeId: "{employeeId}") },
        { "CompanySchedule", "GET", $"{CompanySchedulePath}?date={Date}" },
    };

    private static JsonNode ToBlock(string blockRange)
    {
        var parts = blockRange.Split(separator: '-');

        return new JsonObject { ["start"] = parts[0], ["end"] = parts[1] };
    }
}
