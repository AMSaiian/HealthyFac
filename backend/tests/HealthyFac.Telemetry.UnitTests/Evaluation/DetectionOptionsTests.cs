using HealthyFac.Telemetry.Application.Evaluation;
using HealthyFac.Telemetry.UnitTests.Evaluation.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.UnitTests.Evaluation;

[Feature(TestFeatures.Evaluation)]
public sealed class DetectionOptionsTests
{
    [Fact]
    public void Validate_WithTheConfiguredValues_Passes()
    {
        var options = DetectionOptionsTestData.CreateValid();

        var result = new DetectionOptionsValidator().Validate(options);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(DetectionOptionsTestData.BrokenCrossRules), MemberType = typeof(DetectionOptionsTestData))]
    public void Validate_WithBrokenCrossRule_FailsWithItsMessage(
        string caseName,
        DetectionOptions options,
        string message
    )
    {
        var result = new DetectionOptionsValidator().Validate(options);

        result.Errors.Should().ContainSingle(caseName).Which.ErrorMessage.Should().Be(message);
    }
}
