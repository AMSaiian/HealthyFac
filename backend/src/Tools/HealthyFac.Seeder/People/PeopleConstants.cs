using HealthyFac.Core.Application.Shifts.BlockInput;
using HealthyFac.Core.Domain.Shifts;

namespace HealthyFac.Seeder.People;

/// <summary>Defines the file name, the item keys, and the standard week of the people part.</summary>
public static class PeopleConstants
{
    public const string FileName = "company.json";
    public const string CompanyKey = "company";
    public const string InviteSuffix = ".invite";
    public const string EmployeeSuffix = ".employee";
    public const string UserSuffix = ".user";
    public const string InvitedSuffix = ".invited";
    public const string AcceptedSuffix = ".accepted";
    public const string ShiftSuffix = ".shift";
    public const string OverrideSuffix = ".override";
    public const string CheckToken = "check-token";

    /// <summary>The ID that the seed check puts in a request whose real ID exists only after an event.</summary>
    public static readonly Guid CheckId = Guid.Parse(input: "019b4c00-0000-7000-8000-0000000000ff");

    /// <summary>The time from the accept of an invite to the standard week that the inviter creates.</summary>
    public static readonly TimeSpan ShiftDelay = TimeSpan.FromMinutes(minutes: 30);

    public static readonly IReadOnlyList<Weekday> Weekend = [Weekday.Saturday, Weekday.Sunday];

    public static readonly IReadOnlyList<Weekday> Workdays =
        [Weekday.Monday, Weekday.Tuesday, Weekday.Wednesday, Weekday.Thursday, Weekday.Friday];

    /// <summary>The blocks of a standard day: 08:00 to 12:00 and 13:00 to 17:00.</summary>
    public static readonly IReadOnlyList<ShiftBlockInput> StandardBlocks =
    [
        new() { Start = new TimeOnly(hour: 8, minute: 0), End = new TimeOnly(hour: 12, minute: 0) },
        new() { Start = new TimeOnly(hour: 13, minute: 0), End = new TimeOnly(hour: 17, minute: 0) },
    ];
}
