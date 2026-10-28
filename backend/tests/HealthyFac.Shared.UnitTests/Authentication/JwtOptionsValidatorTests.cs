using HealthyFac.Shared.UnitTests.Authentication.Data;
using HealthyFac.Shared.Web.Authentication;

namespace HealthyFac.Shared.UnitTests.Authentication;

public sealed class JwtOptionsValidatorTests
{
    private readonly JwtOptionsValidator _validator = new();

    [Fact]
    public void Validate_WithAllSettings_IsValid()
    {
        var result = _validator.Validate(AuthenticationTestData.ValidOptions);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(AuthenticationTestData.InvalidOptions), MemberType = typeof(AuthenticationTestData))]
    public void Validate_WithInvalidSetting_ReportsOnlyThatSetting(JwtOptions options, string message)
    {
        var result = _validator.Validate(options);

        result.Errors.Should().ContainSingle(error => error.ErrorMessage == message);
    }
}
