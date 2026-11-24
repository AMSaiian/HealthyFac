using HealthyFac.Shared.Application.CurrentUser.Actors;

namespace HealthyFac.Seeder.IntegrationTests.Relay.Data;

public static class RelayTestData
{
    public const string CompanyName = "Kalyna Works";
    public const string CompanyTimeZoneId = "Europe/Kyiv";
    public const string MachineName = "Press 1";
    public const string SerialNumber = "PR-0001";
    public const string TelemetryService = "telemetry";

    public static readonly DateOnly GoLiveOn = new(year: 2027, month: 4, day: 1);

    public static ActorContext ActorOf(Guid companyId) => new()
    {
        Kind = ActorKind.User,
        UserId = Guid.Parse(input: "019b4c00-0000-7000-8000-00000000a001"),
        CompanyId = companyId,
    };
}
