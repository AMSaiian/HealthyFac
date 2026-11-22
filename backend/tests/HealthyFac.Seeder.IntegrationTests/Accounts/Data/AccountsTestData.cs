namespace HealthyFac.Seeder.IntegrationTests.Accounts.Data;

public static class AccountsTestData
{
    public const string Email = "anna.test@kalyna-works.test";
    public const string FirstName = "Anna";
    public const string LastName = "Test";
    public const string Password = "Kalyna-Demo-2026!";
    public const string OrganizationName = "Kalyna Precision Works";
    public const string AccessTokenProperty = "access_token";
    public const int MemberCount = 11;

    public static readonly Guid CompanyId = Guid.Parse(input: "019b4c00-0000-7000-8000-00000000c0aa");
    public static readonly Guid EmployeeId = Guid.Parse(input: "019b4c00-0000-7000-8000-00000000e0aa");
}
