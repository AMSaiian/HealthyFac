using System.Text.Json;
using HealthyFac.Shared.Infrastructure.Messaging;
using HealthyFac.Shared.UnitTests.Messaging.Data;

namespace HealthyFac.Shared.UnitTests.Messaging;

public sealed class MessagingJsonOptionsTests
{
    [Fact]
    public void Serialize_WithEnumerationMember_WritesItsCamelCaseValue()
    {
        var json = JsonSerializer.Serialize(StateMessageTestData.Message, MessagingJsonOptions.Instance);

        json.Should().Be(StateMessageTestData.StateJson);
    }

    [Fact]
    public void Deserialize_WithEnumerationValue_ReadsTheMember()
    {
        var message = JsonSerializer.Deserialize<StateMessage>(
            StateMessageTestData.StateJson,
            MessagingJsonOptions.Instance);

        message.Should().Be(StateMessageTestData.Message);
    }

    [Fact]
    public void Deserialize_WithUnknownEnumerationValue_ThrowsJsonException()
    {
        var act = () => JsonSerializer.Deserialize<StateMessage>(
            StateMessageTestData.UnknownStateJson,
            MessagingJsonOptions.Instance);

        act.Should().Throw<JsonException>();
    }
}
