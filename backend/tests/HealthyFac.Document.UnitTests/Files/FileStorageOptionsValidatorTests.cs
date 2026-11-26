using HealthyFac.Document.Infrastructure.Files.Storage;
using HealthyFac.Document.UnitTests.Files.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Document.UnitTests.Files;

[Feature(TestFeatures.Files)]
public sealed class FileStorageOptionsValidatorTests
{
    private readonly FileStorageOptionsValidator _validator = new();

    [Theory]
    [MemberData(
        nameof(FileStorageOptionsValidatorTestData.InvalidOptions),
        MemberType = typeof(FileStorageOptionsValidatorTestData)
    )]
    public void Validate_WithInvalidSetting_ReportsThatSetting(FileStorageOptions options, string property)
    {
        var result = _validator.Validate(options);

        result.Errors.Should().ContainSingle(error => error.PropertyName == property);
    }

    [Theory]
    [MemberData(
        nameof(FileStorageOptionsValidatorTestData.ValidOptions),
        MemberType = typeof(FileStorageOptionsValidatorTestData)
    )]
    public void Validate_WithValidSettings_IsValid(FileStorageOptions options)
    {
        var result = _validator.Validate(options);

        result.IsValid.Should().BeTrue();
    }
}
