namespace HealthyFac.Seeder.IntegrationTests.Reset.Data;

public static class SeedResetTestData
{
    public const string ResetArgument = "--reset";
    public const string ExtraBlobName = "extra-blob";
    public const string ExtraEmail = "extra.member@kalyna-works.test";
    public const string ExtraFirstName = "Extra";
    public const string ExtraLastName = "Member";
    public const string ExtraPassword = "Extra-Member-2026!";
    public const string CoreDatabase = "seeder_core";
    public const string StopHint = "docker compose stop core telemetry document insights auth";
    public const string CompanyCreatedAt = "d-61 08:00";
    public const int BlobCount = 6;
    public const int LaterAnchorDays = 7;
}
