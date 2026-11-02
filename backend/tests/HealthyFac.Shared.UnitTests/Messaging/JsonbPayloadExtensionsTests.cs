using HealthyFac.Shared.Infrastructure.Messaging.Persistence;
using HealthyFac.Shared.UnitTests.Messaging.Data;

namespace HealthyFac.Shared.UnitTests.Messaging;

public sealed class JsonbPayloadExtensionsTests
{
    [Theory]
    [MemberData(
        nameof(JsonbPayloadTestData.StorablePayloads),
        MemberType = typeof(JsonbPayloadTestData)
    )]
    public void IsStorableAsJsonb_WithValidJson_ReturnsTrue(string payload)
    {
        var isStorable = payload.IsStorableAsJsonb();

        isStorable.Should().BeTrue();
    }

    [Theory]
    [MemberData(
        nameof(JsonbPayloadTestData.NotStorablePayloads),
        MemberType = typeof(JsonbPayloadTestData)
    )]
    public void IsStorableAsJsonb_WithInvalidText_ReturnsFalse(string payload)
    {
        var isStorable = payload.IsStorableAsJsonb();

        isStorable.Should().BeFalse();
    }
}
