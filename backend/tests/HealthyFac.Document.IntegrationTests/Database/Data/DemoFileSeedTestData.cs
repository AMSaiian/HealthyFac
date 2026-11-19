using HealthyFac.Document.Infrastructure.Database.Seeding;
using HealthyFac.Testing.Seeding;
using Microsoft.Extensions.Options;

namespace HealthyFac.Document.IntegrationTests.Database.Data;

public static class DemoFileSeedTestData
{
    public const int FileCount = 3;
    public static readonly Guid SupervisorId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000e21");
    public static readonly Guid CncMachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000101");
    public static readonly Guid CncFileId = Guid.Parse(input: "01a1efde-2242-73fb-8578-c4961231a06f");

    public static readonly DateTimeOffset SeedTime =
        new(year: 2030, month: 5, day: 1, hour: 12, minute: 0, second: 0, TimeSpan.Zero);

    public static readonly DateTime CncIngestStateChangedAtInSnapshot =
        DateTimeOffset.Parse(input: "2026-11-18T18:17:45.764381+00:00").UtcDateTime;

    public static readonly DateTime CncMachineChangedAtInSnapshot =
        DateTimeOffset.Parse(input: "2026-11-18T18:15:13.282088+00:00").UtcDateTime;

    public static readonly DateTimeOffset ReferenceTimeInSnapshot =
        DateTimeOffset.Parse(input: "2026-11-18T18:19:52.974308+00:00");

    public static string Folder => Path.Combine(Path.GetDirectoryName(SeedDataFile.Find())!, path2: "demo-ingest");

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
