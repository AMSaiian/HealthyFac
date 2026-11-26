using HealthyFac.Telemetry.Messaging.Signals;
using HealthyFac.Telemetry.UnitTests.Messaging.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.UnitTests.Messaging;

[Feature(TestFeatures.Messaging)]
[Feature(TestFeatures.Evaluation)]
public sealed class SignalEpisodeChangedTests
{
    [Theory]
    [MemberData(
        nameof(SignalEpisodeChangedTestData.InvalidMessages), MemberType = typeof(SignalEpisodeChangedTestData))]
    public void Validate_WithABrokenCrossRule_FailsWithItsMessage(
        string caseName,
        SignalEpisodeChanged message,
        string errorMessage
    )
    {
        var result = new SignalEpisodeChangedValidator().Validate(message);

        result.Errors.Should().ContainSingle(caseName).Which.ErrorMessage.Should().Be(errorMessage);
    }

    [Theory]
    [MemberData(nameof(SignalEpisodeChangedTestData.ValidMessages), MemberType = typeof(SignalEpisodeChangedTestData))]
    public void Validate_WithAConsistentMessage_Passes(string caseName, SignalEpisodeChanged message)
    {
        var result = new SignalEpisodeChangedValidator().Validate(message);

        result.IsValid.Should().BeTrue(caseName);
    }
}
