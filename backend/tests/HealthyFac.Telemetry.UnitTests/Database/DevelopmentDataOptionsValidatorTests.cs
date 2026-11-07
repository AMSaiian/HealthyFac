using HealthyFac.Telemetry.Infrastructure.Database.Seeding;
using HealthyFac.Telemetry.UnitTests.Database.Data;

namespace HealthyFac.Telemetry.UnitTests.Database;

public sealed class DevelopmentDataOptionsValidatorTests
{
    private readonly DevelopmentDataOptionsValidator _validator = new();

    [Fact]
    public void Validate_WithoutTestTenant_IsValid()
    {
        var result = _validator.Validate(new DevelopmentDataOptions());

        result.IsValid.Should().BeTrue();
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
}
