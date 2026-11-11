namespace HealthyFac.Core.ApiTests.Employees.Data;

public static class EmployeeInviteApiTestData
{
    public const string CreateInvitePath = "v1/employee-invites";
    public const string RevokeInvitePathFormat = "v1/employee-invites/{0}?rowVersion={1}";
    public const string NotAnEncryptedId = "not-an-encrypted-id";
    public const string VerifyInvitePath = "v1/employee-invites/verify";
    public const string AcceptInvitePath = "v1/employee-invites/accept";
    public const string AcceptedFirstName = "Anna";
    public const string AcceptedLastName = "Bondar";
    public const string WorkerRoleCode = "worker";
    public const string UnknownInviteToken = "unknown-invite-token";
    public const string OtherAudience = "healthyfac-other-api";
    public const string EmailField = "email";
    public const string FirstNameField = "firstName";
    public const string NullCharacterEmail = "before\0after@healthyfac.test";
    public const string NullCharacterEmailDomain = "after@healthyfac.test";
    public const string LowerCaseEmail = "mixed.case@healthyfac.test";
    public const string MixedCaseEmail = "  Mixed.Case@HealthyFac.Test ";

    public static readonly Guid ServiceCallerUserId = Guid.Parse(input: "0190f3a0-0000-7000-8000-0000000000d1");
    public static readonly Guid OtherCompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-0000000000c9");
}
