using HealthyFac.Shared.Infrastructure.Email;
using HealthyFac.Shared.UnitTests.Email.Data;

namespace HealthyFac.Shared.UnitTests.Email;

public sealed class SmtpOptionsValidatorTests
{
    private readonly SmtpOptionsValidator _validator = new();

    [Theory]
    [MemberData(
        nameof(SmtpOptionsValidatorTestData.ValidOptions),
        MemberType = typeof(SmtpOptionsValidatorTestData)
    )]
    public void Validate_WithValidOptions_IsValid(SmtpOptions options)
    {
        var result = _validator.Validate(options);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [MemberData(
        nameof(SmtpOptionsValidatorTestData.InvalidOptionsWithExpectedError),
        MemberType = typeof(SmtpOptionsValidatorTestData)
    )]
    public void Validate_WithInvalidOptions_ReportsExpectedError(SmtpOptions options, string expectedErrorMessage)
    {
        var result = _validator.Validate(options);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.ErrorMessage == expectedErrorMessage);
    }
}
