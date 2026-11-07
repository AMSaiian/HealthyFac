using HealthyFac.Telemetry.Infrastructure.Database.Seeding;
using Microsoft.Extensions.Options;

namespace HealthyFac.Telemetry.IntegrationTests.Database.Data;

public static class DevelopmentSeedTestData
{
    public const int StatusDefinitionsPerMachine = 3;
    public const string FirstGatewayId = "seedmachineone22";
    public const string SecondGatewayId = "seedmachinetwo22";

    public static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000e01");
    public static readonly Guid FirstDepartmentId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000f01");
    public static readonly Guid SecondDepartmentId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000f02");
    public static readonly Guid FirstMachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000e11");
    public static readonly Guid SecondMachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000e12");

    public static IOptions<SeedingOptions> CreateOptions() =>
        Options.Create(
            new SeedingOptions
            {
                IsOptionalDataSeeded = true,
                DevelopmentData = new DevelopmentDataOptions
                {
                    StatusDefinitionsPerMachine = StatusDefinitionsPerMachine,
                    TestTenant = new TestTenantOptions
                    {
                        CompanyId = CompanyId,
                        DepartmentIds = [FirstDepartmentId, SecondDepartmentId],
                    },
                    SeedMachines =
                    [
                        new SeedMachineOptions
                        {
                            MachineId = FirstMachineId, GatewayId = FirstGatewayId, DepartmentIndex = 0,
                        },
                        new SeedMachineOptions
                        {
                            MachineId = SecondMachineId, GatewayId = SecondGatewayId, DepartmentIndex = 1,
                        },
                    ],
                },
            });
}
