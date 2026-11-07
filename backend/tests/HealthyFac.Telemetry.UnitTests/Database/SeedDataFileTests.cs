using HealthyFac.Telemetry.Infrastructure.Database.Seeding;
using HealthyFac.Telemetry.UnitTests.Database.Data;
using HealthyFac.Testing.Seeding;
using Microsoft.Extensions.Configuration;

namespace HealthyFac.Telemetry.UnitTests.Database;

public sealed class SeedDataFileTests
{
    private const string TestTenantPath =
        $"{SeedingOptions.SectionName}:{DevelopmentDataOptions.SectionName}:TestTenant";

    [Fact]
    public void SeedDataFile_Development_PassesTelemetryValidators()
    {
        var testTenant = DevelopmentDataOptionsValidatorTestData.ValidTestTenant;
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(SeedDataFile.Find(), optional: false)
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [$"{TestTenantPath}:CompanyId"] = testTenant.CompanyId.ToString(),
                    [$"{TestTenantPath}:DepartmentIds:0"] = testTenant.DepartmentIds[index: 0].ToString(),
                    [$"{TestTenantPath}:DepartmentIds:1"] = testTenant.DepartmentIds[index: 1].ToString(),
                })
            .Build();
        var options = configuration.GetSection(SeedingOptions.SectionName).Get<SeedingOptions>()!;

        var result = new DevelopmentDataOptionsValidator().Validate(options.DevelopmentData);

        result.Errors.Should().BeEmpty();
        options.DevelopmentData.GetSeedMachines().Should().NotBeEmpty();
    }
}
