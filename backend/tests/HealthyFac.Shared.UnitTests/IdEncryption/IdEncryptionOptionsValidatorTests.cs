using HealthyFac.Shared.Infrastructure.IdEncryption;
using HealthyFac.Shared.UnitTests.IdEncryption.Data;

namespace HealthyFac.Shared.UnitTests.IdEncryption;

public sealed class IdEncryptionOptionsValidatorTests
{
    private readonly IdEncryptionOptionsValidator _validator = new();

    [Theory]
    [MemberData(
        nameof(IdEncryptionOptionsValidatorTestData.ValidKeys),
        MemberType = typeof(IdEncryptionOptionsValidatorTestData)
    )]
    public void Validate_WhenKeyIsValid_ShouldBeValid(string key)
    {
        var options = BuildOptions(key);

        var result = _validator.Validate(options);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [MemberData(
        nameof(IdEncryptionOptionsValidatorTestData.InvalidKeysWithExpectedError),
        MemberType = typeof(IdEncryptionOptionsValidatorTestData)
    )]
    public void Validate_WhenKeyIsInvalid_ShouldHaveExpectedValidationError(string key, string expectedErrorMessage)
    {
        var options = BuildOptions(key);

        var result = _validator.Validate(options);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage == expectedErrorMessage);
    }

    [Fact]
    public void Validate_WhenNoKeysConfigured_ShouldReportKeysRequired()
    {
        var result = _validator.Validate(new IdEncryptionOptions());

        result.IsValid.Should().BeFalse();
        result.Errors.Should()
            .Contain(error => error.ErrorMessage == IdEncryptionValidationMessages.Keys.Required);
    }

    private static IdEncryptionOptions BuildOptions(string key) =>
        new()
        {
            Keys =
            [
                new IdEncryptionKeyOptions
                {
                    Prefix = IdEncryptionOptionsValidatorTestData.DefaultPrefix,
                    Key = key,
                    Default = true,
                },
            ],
        };
}
