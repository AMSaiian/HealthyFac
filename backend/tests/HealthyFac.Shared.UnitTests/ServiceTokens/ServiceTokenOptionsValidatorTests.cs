using HealthyFac.Shared.Infrastructure.ServiceTokens;
using HealthyFac.Shared.UnitTests.ServiceTokens.Data;

namespace HealthyFac.Shared.UnitTests.ServiceTokens;

public sealed class ServiceTokenOptionsValidatorTests
{
    private readonly ServiceTokenOptionsValidator _validator = new();

    [Theory]
    [MemberData(
        nameof(ServiceTokenTestData.ValidOptions),
        MemberType = typeof(ServiceTokenTestData)
    )]
    public void Validate_WithValidOptions_IsValid(ServiceTokenOptions options)
    {
        var result = _validator.Validate(options);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(
        nameof(ServiceTokenTestData.InvalidOptionsWithExpectedError),
        MemberType = typeof(ServiceTokenTestData)
    )]
    public void Validate_WithInvalidOptions_ReportsExpectedError(
        ServiceTokenOptions options,
        string expectedErrorMessage
    )
    {
        var result = _validator.Validate(options);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.ErrorMessage == expectedErrorMessage);
    }
}
