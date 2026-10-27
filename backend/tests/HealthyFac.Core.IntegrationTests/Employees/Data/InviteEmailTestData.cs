namespace HealthyFac.Core.IntegrationTests.Employees.Data;

public static class InviteEmailTestData
{
    public const string OpenInviteEmail = "email-open@plant.com";
    public const string RenewedInviteEmail = "email-renewed@plant.com";
    public const string AcceptedInviteEmail = "email-accepted@plant.com";
    public const string UnknownInviteEmail = "email-unknown@plant.com";
    public const string ExpiredInviteEmail = "email-expired@plant.com";
    public const string SenderFailureEmail = "email-failure@plant.com";
    public const string TrailingSlashEmail = "email-slash@plant.com";

    public const string ConfiguredSpaBaseUrl = "https://app.test";
    public const string TrailingSlashSpaBaseUrl = "https://app.example/";
    public const string TrailingSlashExpectedPrefix = "https://app.example/invite#token=";
    public const string TokenMarker = "token=";
    public const string PlainSubject = "Plain invite email";

    public const string SenderFailureMessage = "The SMTP server is down.";
}
