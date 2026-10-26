using HealthyFac.Gateway.IntegrationTests.RateLimiting.Data;
using HealthyFac.Gateway.Web.RateLimiting;

namespace HealthyFac.Gateway.IntegrationTests.RateLimiting;

public sealed class RateLimitOptionsValidatorTests
{
    private readonly RateLimitOptionsValidator _validator = new();

    [Fact]
    public void Validate_WithDefaultOptions_ShouldBeValid()
    {
        var result = _validator.Validate(new RateLimitOptions());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [MemberData(
        nameof(RateLimitOptionsValidatorTestData.InvalidOptionsWithExpectedError),
        MemberType = typeof(RateLimitOptionsValidatorTestData)
    )]
    public void Validate_WhenALimitIsBelowOne_ShouldReportExpectedError(
        RateLimitOptions options,
        string expectedErrorMessage
    )
    {
        var result = _validator.Validate(options);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.ErrorMessage == expectedErrorMessage);
    }
}
