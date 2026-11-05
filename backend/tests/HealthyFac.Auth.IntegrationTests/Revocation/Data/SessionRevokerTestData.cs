namespace HealthyFac.Auth.IntegrationTests.Revocation.Data;

public static class SessionRevokerTestData
{
    public static readonly Guid UserId = Guid.Parse(input: "0192f0c4-7a3e-7b10-9c2d-4e5f6a7b8c9d");

    public static readonly DateTimeOffset RevokedAt = new(
        year: 2026, month: 11, day: 2, hour: 10, minute: 0, second: 0, TimeSpan.Zero);
}
