using HealthyFac.Core.Application.Machines.SearchMachines;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Shared.Application.Grids;

namespace HealthyFac.Core.IntegrationTests.Machines.Data;

public static class MachineTestData
{
    public const string CompanyName = "Plant Co";
    public const string OtherCompanyName = "Other Plant Co";
    public const string FirstDepartmentName = "Assembly";
    public const string SecondDepartmentName = "Packaging";

    public const string MachineName = "Lathe 01";
    public const string OtherMachineName = "Press 02";
    public const string SerialNumber = "ABC-1";
    public const string SerialNumberOtherCase = "abc-1";
    public const string OtherSerialNumber = "XYZ-2";
    public const string ThirdSerialNumber = "QRS-3";

    public const string BulletListDescription =
        "<ol><li data-list=\"bullet\"><span class=\"ql-ui\" contenteditable=\"false\"></span>a</li></ol>";

    public const string LinkDescription =
        "<p><a href=\"https://example.com\" rel=\"noopener noreferrer\" target=\"_blank\">a</a></p>";

    public const string HostileDescription = "<p>a</p><script>alert(1)</script>";

    public const int DescriptionLimit = 5000;
    public const int PageSize = 2;
    public const int SecondPageOffset = 2;
    public const string LowerNamePrefix = "lathe";
    public const string FilterNameBase = "Lathe";
    public const string OtherFilterNameBase = "Press";
    public const int SeededMachineCount = 6;
    public const int RaceParticipantCount = 2;

    public static readonly DateTime FirstCreatedAt = new(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc);

    public static string TooLongDescription => new('a', DescriptionLimit + 1);

    public static SearchMachinesRequest CreateSearch(
        Guid companyId,
        MachineGridFilter? filter = null,
        GridSort<MachineSortColumn>? sort = null,
        int offset = 0,
        int limit = 25
    ) =>
        new()
        {
            CompanyId = companyId,
            Offset = offset,
            Limit = limit,
            Sort = sort,
            Filter = filter ?? new MachineGridFilter(),
        };

    public static IReadOnlyList<MachineSeed> CreateFilterSeeds() =>
    [
        new(FilterNameBase + " 01", "S-01", MachineState.Operational, FirstCreatedAt),
        new(FilterNameBase + " 02", "S-02", MachineState.Operational, FirstCreatedAt.AddDays(1)),
        new(FilterNameBase + " 03", "S-03", MachineState.Operational, FirstCreatedAt.AddDays(2)),
        new(FilterNameBase + " 04", "S-04", MachineState.Operational, FirstCreatedAt.AddDays(3)),
        new(FilterNameBase + " 05", "S-05", MachineState.InMaintenance, FirstCreatedAt.AddDays(4)),
        new(OtherFilterNameBase + " 01", "S-06", MachineState.Operational, FirstCreatedAt.AddDays(5)),
    ];
}

public sealed record MachineSeed(string Name, string SerialNumber, MachineState State, DateTime CreatedAt);
