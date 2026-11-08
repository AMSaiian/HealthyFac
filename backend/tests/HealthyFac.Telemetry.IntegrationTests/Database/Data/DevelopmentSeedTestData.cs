using HealthyFac.Telemetry.Infrastructure.Database.Seeding;
using Microsoft.Extensions.Options;

namespace HealthyFac.Telemetry.IntegrationTests.Database.Data;

public static class DevelopmentSeedTestData
{
    public const int StatusDefinitionsPerMachine = 3;
    public const string FirstGatewayId = "seedmachineone22";
    public const string SecondGatewayId = "seedmachinetwo22";

    public static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000e01");
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
                    },
                    SeedMachines =
                    [
                        new SeedMachineOptions
                        {
                            MachineId = FirstMachineId, GatewayId = FirstGatewayId,
                        },
                        new SeedMachineOptions
                        {
                            MachineId = SecondMachineId, GatewayId = SecondGatewayId,
                        },
                    ],
                },
            });
}
