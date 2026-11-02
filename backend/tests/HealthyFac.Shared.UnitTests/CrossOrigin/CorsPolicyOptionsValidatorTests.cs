using HealthyFac.Shared.UnitTests.CrossOrigin.Data;
using HealthyFac.Shared.Web.CrossOrigin;

namespace HealthyFac.Shared.UnitTests.CrossOrigin;

public sealed class CorsPolicyOptionsValidatorTests
{
    private readonly CorsPolicyOptionsValidator _validator = new();

    [Fact]
    public void Validate_WhenEnabledWithoutPreflightMaxAge_Fails()
    {
        var result = _validator.Validate(CorsPolicyOptionsValidatorTestData.EnabledWithPreflightMaxAge(0));

        result.Errors.Should()
            .ContainSingle(error => error.ErrorMessage == CorsValidationMessages.PreflightMaxAgeRequired);
    }

    [Theory]
    [MemberData(
        nameof(CorsPolicyOptionsValidatorTestData.AboveBrowserLimit),
        MemberType = typeof(CorsPolicyOptionsValidatorTestData)
    )]
    public void Validate_WithPreflightMaxAgeAboveBrowserLimit_Fails(int seconds)
    {
        var result = _validator.Validate(CorsPolicyOptionsValidatorTestData.EnabledWithPreflightMaxAge(seconds));

        result.Errors.Should()
            .ContainSingle(error => error.ErrorMessage == CorsPolicyOptionsValidatorTestData.AboveBrowserLimitMessage);
    }

    [Theory]
    [MemberData(
        nameof(CorsPolicyOptionsValidatorTestData.ValidPreflightMaxAges),
        MemberType = typeof(CorsPolicyOptionsValidatorTestData)
    )]
    public void Validate_WithValidPreflightMaxAge_Passes(int seconds)
    {
        var result = _validator.Validate(CorsPolicyOptionsValidatorTestData.EnabledWithPreflightMaxAge(seconds));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenDisabledWithoutPreflightMaxAge_Passes()
    {
        var result = _validator.Validate(new CorsPolicyOptions());

        result.IsValid.Should().BeTrue();
    }
}
