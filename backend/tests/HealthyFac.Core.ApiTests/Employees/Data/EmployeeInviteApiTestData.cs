namespace HealthyFac.Core.ApiTests.Employees.Data;

public static class EmployeeInviteApiTestData
{
    public const string CreateInvitePath = "v1/employee-invites";
    public const string VerifyInvitePath = "v1/employee-invites/verify";
    public const string WorkerRoleCode = "worker";
    public const string UnknownInviteToken = "unknown-invite-token";
    public const string OtherAudience = "healthyfac-other-api";

    public static readonly Guid OtherCompanyId = Guid.Parse("0190f3a0-0000-7000-8000-0000000000c9");
}
