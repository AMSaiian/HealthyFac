namespace HealthyFac.Core.ApiTests.Rpc.Data;

public static class CoreRpcTestData
{
    public const string MachinesRead = "machines:read";
    public const string WorkOrdersRead = "work-orders:read";
    public const string WorkOrdersReadCompany = "work-orders:read-company";
    public const string WorkOrdersWriteCompany = "work-orders:write-company";
    public const string EmployeesRead = "employees:read";
    public const string ShiftsRead = "shifts:read";
    public const string ShiftsReadCompany = "shifts:read-company";
    public const string ShiftsWriteCompany = "shifts:write-company";
    public const string OtherPermission = "files:read";
    public const string MachineSerial = "RPC-MACHINE-1";
    public const string PaddedMachineSerial = "  RPC-MACHINE-1 ";
    public const string PaddedSearch = "  Wanda ";
    public const string OperationalState = "operational";
    public const string OwnOrderTitle = "Own rpc order";
    public const string BacklogOrderTitle = "Backlog rpc order";
    public const string SecondWorkerEmail = "second.rpc@healthyfac.test";
    public const string MalformedDate = "12.04.2027";
    public const string WeekdayDate = "2027-04-12";
    public const string CreatedAtColumn = "createdAt";
    public const string DescendingDirection = "desc";
    public const int PageLimit = 25;
    public const int HistoryLimit = 5;
    public const int StateRangeHours = 2;
    public const int StateRangeSeconds = 7200;
    public const int BlockCount = 2;

    public static TheoryData<string, bool, bool> MissingStateRangeBounds => new()
    {
        { "NoFrom", false, true },
        { "NoTo", true, false },
    };

    public static readonly string[] SupervisorPermissions =
    [
        MachinesRead, WorkOrdersRead, WorkOrdersReadCompany, WorkOrdersWriteCompany, EmployeesRead, ShiftsRead,
        ShiftsReadCompany, ShiftsWriteCompany,
    ];
}
