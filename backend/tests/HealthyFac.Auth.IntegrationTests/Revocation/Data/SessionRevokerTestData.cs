namespace HealthyFac.Auth.IntegrationTests.Revocation.Data;

public static class SessionRevokerTestData
{
    public static readonly Guid UserId = Guid.Parse("0192f0c4-7a3e-7b10-9c2d-4e5f6a7b8c9d");

    public static readonly DateTimeOffset RevokedAt = new(2026, 11, 2, 10, 0, 0, TimeSpan.Zero);
}
