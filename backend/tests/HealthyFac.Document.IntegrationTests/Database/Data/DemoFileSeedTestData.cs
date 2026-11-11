using HealthyFac.Document.Infrastructure.Database.Seeding;
using HealthyFac.Testing.Seeding;
using Microsoft.Extensions.Options;

namespace HealthyFac.Document.IntegrationTests.Database.Data;

public static class DemoFileSeedTestData
{
    public const int FileCount = 3;
    public static readonly Guid SupervisorId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000e21");
    public static readonly Guid CncMachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000101");
    public static readonly Guid CncFileId = Guid.Parse(input: "01a1c5b1-557a-7039-9339-06a308f1f579");
    public static readonly DateTimeOffset SeedTime =
        new(year: 2030, month: 5, day: 1, hour: 12, minute: 0, second: 0, TimeSpan.Zero);

    public static readonly DateTime CncIngestStateChangedAtInSnapshot =
        DateTimeOffset.Parse(input: "2026-11-10T13:43:57.428455+00:00").UtcDateTime;

    public static readonly DateTime CncMachineChangedAtInSnapshot =
        DateTimeOffset.Parse(input: "2026-11-10T13:42:29.758557+00:00").UtcDateTime;

    public static readonly DateTimeOffset ReferenceTimeInSnapshot =
        DateTimeOffset.Parse(input: "2026-11-10T13:44:48.132126+00:00");

    public static string Folder => Path.Combine(Path.GetDirectoryName(SeedDataFile.Find())!, "demo-ingest");

    public static IOptions<SeedingOptions> CreateOptions(bool hasTenant = true) =>
        Options.Create(
            new SeedingOptions
            {
                IsOptionalDataSeeded = true,
                DevelopmentData = new DevelopmentDataOptions
                {
                    TestTenant = hasTenant
                        ? new TestTenantOptions
                        {
                            CompanyId = DevelopmentSeedTestData.CompanyId,
                            SupervisorIdentityId = SupervisorId,
                        }
                        : null,
                    SeedMachines = [new SeedMachineOptions { MachineId = CncMachineId }],
                },
            });
}
