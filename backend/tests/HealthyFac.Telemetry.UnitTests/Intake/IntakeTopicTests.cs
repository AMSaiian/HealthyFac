using HealthyFac.Telemetry.Application.Intake;
using HealthyFac.Telemetry.UnitTests.Intake.Data;

namespace HealthyFac.Telemetry.UnitTests.Intake;

public sealed class IntakeTopicTests
{
    [Fact]
    public void TryGetGatewayId_WithGatewayTopic_ReturnsTheGatewayId()
    {
        var gatewayId =
            IntakeTopic.TryGetGatewayId($"healthyfac/gateways/{IntakeTopicTestData.ValidGatewayId}/messages");

        gatewayId.Should().Be(IntakeTopicTestData.ValidGatewayId);
    }

    [Theory]
    [MemberData(nameof(IntakeTopicTestData.InvalidTopics), MemberType = typeof(IntakeTopicTestData))]
    public void TryGetGatewayId_WithOtherTopic_ReturnsNull(string topic)
    {
        var gatewayId = IntakeTopic.TryGetGatewayId(topic);

        gatewayId.Should().BeNull();
    }
}
