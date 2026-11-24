namespace HealthyFac.Seeder.IntegrationTests.People.Data;

public static class PeopleTestData
{
    public const string SeedFolder = "seed/demo";
    public const string ZoneId = "Europe/Kyiv";
    public const string CompanyName = "Kalyna Precision Works";
    public const string Password = "Kalyna-Demo-2026!";
    public const string InviteEmailDomain = "@kalyna-works.test";
    public const int PersonCount = 7;
    public const int OpenInviteCount = 2;
    public const int InviteEmailCount = 8;
    public const int OverrideCount = 2;
    public const string TarasEmail = "taras.hnatiuk@kalyna-works.test";
    public const int TarasInvitedDaysBefore = 60;
    public static readonly TimeOnly TarasInvitedTime = new(hour: 10, minute: 0);
    public const string NadiiaUserKey = "nadiia.user";
    public const string EmployeeIdClaim = "employee_id";
    public const string SupervisorPermission = "\"shifts:manage\"";
    public static readonly TimeSpan MomentTolerance = TimeSpan.FromSeconds(seconds: 1);

    public static readonly IReadOnlySet<string> SupervisorEmails = new HashSet<string>
    {
        "nadiia.kravets@kalyna-works.test",
        TarasEmail,
    };

    public static readonly Guid CompanyId = Guid.Parse(input: "019b4c00-0000-7000-8000-00000000c001");

    /// <summary>A Wednesday, so the workday forms and the weekdays of the file do not move.</summary>
    public static readonly DateTimeOffset Anchor = new(
        year: 2027, month: 6, day: 2, hour: 10, minute: 0, second: 0, TimeSpan.Zero);
}
