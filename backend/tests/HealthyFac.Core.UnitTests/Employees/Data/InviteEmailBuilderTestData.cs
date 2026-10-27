using HealthyFac.Core.Application.Employees.SendEmployeeInviteEmail;

namespace HealthyFac.Core.UnitTests.Employees.Data;

public static class InviteEmailBuilderTestData
{
    public const string Recipient = "worker@plant.com";
    public const string Link = "https://app.test/invite#token=abc.def";
    public const string LinkWithAmpersand = "https://app.test/invite?lang=en&ref=mail#token=abc.def";
    public const string AmpersandInText = "lang=en&ref=mail";
    public const string AmpersandEntity = "&amp;";
    public const string ParentName = "test-parent";
    public const string ExpiryInUtc = "2026-11-03 14:30 UTC";

    public static readonly DateTime ExpiresAt = new(2026, 11, 3, 14, 30, 0, DateTimeKind.Utc);

    public static EmployeeInviteEmailModel Model(string link = Link) => new()
    {
        To = Recipient,
        Link = link,
        ExpiresAt = ExpiresAt,
    };
}
