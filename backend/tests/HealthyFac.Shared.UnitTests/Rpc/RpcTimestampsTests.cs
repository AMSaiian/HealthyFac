using HealthyFac.Shared.Infrastructure.Rpc;
using HealthyFac.Shared.UnitTests.Rpc.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Shared.UnitTests.Rpc;

[Feature(TestFeatures.Platform)]
public sealed class RpcTimestampsTests
{
    [Theory]
    [MemberData(nameof(RpcTimestampsTestData.NonUtcTimes), MemberType = typeof(RpcTimestampsTestData))]
    public void ToTimestamp_WithANonUtcKind_Throws(string caseName, DateTime value)
    {
        var act = () => RpcTimestamps.ToTimestamp(value);

        act.Should().Throw<ArgumentException>(caseName);
    }

    [Fact]
    public void ToTimestamp_WithAUtcValue_KeepsTheInstant()
    {
        var value = new DateTime(year: 2026, month: 11, day: 26, hour: 8, minute: 0, second: 0, DateTimeKind.Utc);

        var timestamp = RpcTimestamps.ToTimestamp(value);

        timestamp.ToDateTime().Should().Be(value);
    }
}
