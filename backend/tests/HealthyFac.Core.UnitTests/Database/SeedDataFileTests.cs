using HealthyFac.Core.Infrastructure.Database.Seeding;
using HealthyFac.Core.UnitTests.Database.Data;
using HealthyFac.Testing.Seeding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace HealthyFac.Core.UnitTests.Database;

public sealed class SeedDataFileTests
{
    private const string TestTenantPath =
        $"{SeedingOptions.SectionName}:{DevelopmentDataOptions.SectionName}:TestTenant";

    [Fact]
    public void SeedDataFile_Development_PassesCoreValidators()
    {
        var testTenant = DevelopmentDataOptionsValidatorTestData.ValidTestTenant;
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(SeedDataFile.Find(), optional: false)
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [$"{TestTenantPath}:CompanyId"] =
                        testTenant.CompanyId.ToString(),
                    [$"{TestTenantPath}:DepartmentIds:0"] =
                        testTenant.DepartmentIds[index: 0].ToString(),
                    [$"{TestTenantPath}:DepartmentIds:1"] =
                        testTenant.DepartmentIds[index: 1].ToString(),
                    [$"{TestTenantPath}:SupervisorIdentityId"] =
                        testTenant.SupervisorIdentityId.ToString(),
                    [$"{TestTenantPath}:SupervisorEmployeeId"] =
                        testTenant.SupervisorEmployeeId.ToString(),
                    [$"{TestTenantPath}:SupervisorEmail"] =
                        testTenant.SupervisorEmail,
                })
            .Build();
        var options = configuration.GetSection(SeedingOptions.SectionName).Get<SeedingOptions>()!;
        var validator = new DevelopmentDataOptionsValidator(
            new HostingEnvironment { EnvironmentName = Environments.Development });

        var result = validator.Validate(options.DevelopmentData);

        result.Errors.Should().BeEmpty();
        options.DevelopmentData.GetSeedMachines().Should().NotBeEmpty();
    }
}
