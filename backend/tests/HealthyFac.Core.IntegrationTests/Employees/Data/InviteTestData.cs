namespace HealthyFac.Core.IntegrationTests.Employees.Data;

public static class InviteTestData
{
    public const string CompanyName = "Plant Co";
    public const string FirstDepartmentName = "Assembly";
    public const string SecondDepartmentName = "Packaging";
    public const string DepartmentTimeZoneId = "Europe/Kyiv";

    public const string RawEmail = " Ann@Plant.COM ";
    public const string NormalizedEmail = "ann@plant.com";
    public const string OutboxEmail = "outbox@plant.com";
    public const string ConflictEmail = "ann2@plant.com";
    public const string ConflictEmailOtherCase = "ANN2@plant.com";
    public const string EmployedEmail = "employed@plant.com";
    public const string ForeignDepartmentEmail = "foreign@plant.com";
    public const string DuplicateDepartmentEmail = "duplicate@plant.com";
    public const string ResendExpiredEmail = "resend-expired@plant.com";
    public const string ResendOutboxEmail = "resend-outbox@plant.com";
    public const string ResendTokenEmail = "resend-token@plant.com";
    public const string ResendAcceptedEmail = "resend-accepted@plant.com";
    public const string ResendOtherCompanyEmail = "resend-other@plant.com";
    public const string VerifyValidEmail = "verify-valid@plant.com";
    public const string VerifyExpiredEmail = "verify-expired@plant.com";
    public const string VerifyAcceptedEmail = "verify-accepted@plant.com";
    public const string VerifyTamperedEmail = "verify-tampered@plant.com";
    public const string VerifyResendEmail = "verify-resend@plant.com";
    public const string AcceptValidEmail = "accept-valid@plant.com";
    public const string AcceptOutboxEmail = "accept-outbox@plant.com";
    public const string AcceptTwiceEmail = "accept-twice@plant.com";
    public const string AcceptExpiredEmail = "accept-expired@plant.com";
    public const string AcceptParallelEmail = "accept-parallel@plant.com";
    public const string AcceptSharedEmail = "accept-shared@plant.com";
    public const string AcceptBlankNameEmail = "accept-blank@plant.com";
    public const string AcceptSharedParallelEmail = "accept-shared-parallel@plant.com";
    public const string CreateParallelEmail = "create-parallel@plant.com";
    public const string ResendParallelEmail = "resend-parallel@plant.com";
    public const string InvalidEmail = "not-an-email";
    public const string ValidEmail = "valid@plant.com";

    public const string EmployeeFirstName = "Eva";
    public const string EmployeeLastName = "Stone";

    public const int ExpectedSaltBytes = 32;
    public const int ExpectedLifetimeHours = 72;
}
