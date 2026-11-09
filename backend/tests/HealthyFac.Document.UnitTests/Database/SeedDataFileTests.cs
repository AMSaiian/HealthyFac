using HealthyFac.Document.Infrastructure.Database.Seeding;
using HealthyFac.Document.UnitTests.Database.Data;
using HealthyFac.Testing.Seeding;
using Microsoft.Extensions.Configuration;

namespace HealthyFac.Document.UnitTests.Database;

public sealed class SeedDataFileTests
{
    private const string TestTenantPath =
        $"{SeedingOptions.SectionName}:{DevelopmentDataOptions.SectionName}:TestTenant";

    [Fact]
    public void SeedDataFile_Development_PassesDocumentValidators()
    {
        var testTenant = DevelopmentDataOptionsValidatorTestData.ValidTestTenant;
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(SeedDataFile.Find(), optional: false)
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [$"{TestTenantPath}:CompanyId"] = testTenant.CompanyId.ToString(),
                })
            .Build();
        var options = configuration.GetSection(SeedingOptions.SectionName).Get<SeedingOptions>()!;

        var result = new DevelopmentDataOptionsValidator().Validate(options.DevelopmentData);

        result.Errors.Should().BeEmpty();
        options.DevelopmentData.GetSeedMachines().Should().NotBeEmpty();
    }
}
