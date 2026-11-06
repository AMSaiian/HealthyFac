using HealthyFac.Shared.Application.Logging;
using HealthyFac.Shared.UnitTests.Logging.Data;

namespace HealthyFac.Shared.UnitTests.Logging;

public sealed class SensitiveDataMaskerTests
{
    [Theory]
    [MemberData(
        nameof(SensitiveDataMaskerTestData.MaskScenariosData),
        MemberType = typeof(SensitiveDataMaskerTestData)
    )]
    public void Mask_WithVariousAttributesAndInputs_ReturnsExpectedMaskedResult(
        string? input,
        SensitiveDataAttribute attribute,
        string? expected
    )
    {
        SensitiveDataMasker.Mask(input, attribute).Should().Be(expected);
    }

    [Fact]
    public void Mask_WithNonStringObject_ConvertsAndMasksSuccessfully()
    {
        var attribute = new SensitiveDataAttribute
        {
            ShowLast = 4,
        };

        SensitiveDataMasker.Mask(value: 123456789, attribute).Should().Be(expected: "***MASKED***6789");
    }

    [Fact]
    public void Mask_WithLengthPreservedAndZeroShowOffsets_ReplacesAllCharactersWithMaskChar()
    {
        var attribute = new SensitiveDataAttribute
        {
            IsLengthPreserved = true,
        };

        SensitiveDataMasker.Mask(value: "secret123", attribute).Should().Be(expected: "*********");
    }
}
