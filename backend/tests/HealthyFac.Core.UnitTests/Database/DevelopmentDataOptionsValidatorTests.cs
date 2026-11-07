using HealthyFac.Core.Infrastructure.Database.Seeding;
using HealthyFac.Core.UnitTests.Database.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace HealthyFac.Core.UnitTests.Database;

public sealed class DevelopmentDataOptionsValidatorTests
{
    private readonly DevelopmentDataOptionsValidator _validator =
        new(new HostingEnvironment { EnvironmentName = Environments.Development });

    [Fact]
    public void Validate_WithoutTestTenant_IsValid()
    {
        var result = _validator.Validate(new DevelopmentDataOptions());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithValidTestTenant_IsValid()
    {
        var result = _validator.Validate(
            new DevelopmentDataOptions { TestTenant = DevelopmentDataOptionsValidatorTestData.ValidTestTenant });

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(
        nameof(SeedMachinesValidatorTestData.ValidSeedMachines),
        MemberType = typeof(SeedMachinesValidatorTestData)
    )]
    public void Validate_WithValidSeedMachines_IsValid(IReadOnlyList<SeedMachineOptions> seedMachines)
    {
        var result = _validator.Validate(
            new DevelopmentDataOptions
            {
                TestTenant = DevelopmentDataOptionsValidatorTestData.ValidTestTenant,
                SeedMachines = seedMachines,
            });

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(
        nameof(SeedMachinesValidatorTestData.InvalidSeedMachines),
        MemberType = typeof(SeedMachinesValidatorTestData)
    )]
    public void Validate_WithInvalidSeedMachines_ReportsThatRule(
        IReadOnlyList<SeedMachineOptions> seedMachines,
        string message
    )
    {
        var result = _validator.Validate(
            new DevelopmentDataOptions
            {
                TestTenant = DevelopmentDataOptionsValidatorTestData.ValidTestTenant,
                SeedMachines = seedMachines,
            });

        result.Errors.Should().ContainSingle(error => error.ErrorMessage == message);
    }

    [Fact]
    public void Validate_WithSeedMachinesAndNoTestTenant_ReportsDepartmentIndex()
    {
        var result = _validator.Validate(
            new DevelopmentDataOptions { SeedMachines = [SeedMachinesValidatorTestData.SingleSeedMachine] });

        result.Errors.Should()
            .ContainSingle(error =>
                error.ErrorMessage == SeedingValidationConstants.Messages.SeedMachineDepartmentIndexOutOfRange);
    }

    [Theory]
    [MemberData(
        nameof(DevelopmentDataOptionsValidatorTestData.InvalidTestTenants),
        MemberType = typeof(DevelopmentDataOptionsValidatorTestData)
    )]
    public void Validate_WithInvalidTestTenantSetting_ReportsThatSetting(TestTenantOptions testTenant, string property)
    {
        var result = _validator.Validate(new DevelopmentDataOptions { TestTenant = testTenant });

        result.Errors.Should().ContainSingle(error => error.PropertyName.EndsWith(property));
    }
}
