using HealthyFac.Telemetry.UnitTests.Intake.Data;

namespace HealthyFac.Telemetry.UnitTests.Intake;

public sealed class ReceivedMessageTests
{
    [Theory]
    [MemberData(nameof(ReceivedMessageTestData.InvalidPayloadForms), MemberType = typeof(ReceivedMessageTestData))]
    public void Constructor_WithBothOrNeitherPayload_Throws(byte[]? rawPayload, string? payload)
    {
        var act = () => ReceivedMessageTestData.Create(rawPayload, payload);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [MemberData(nameof(ReceivedMessageTestData.ValidPayloadForms), MemberType = typeof(ReceivedMessageTestData))]
    public void Constructor_WithExactlyOnePayloadForm_Succeeds(byte[]? rawPayload, string? payload)
    {
        var act = () => ReceivedMessageTestData.Create(rawPayload, payload);

        act.Should().NotThrow();
    }
}
