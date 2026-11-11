using System.Globalization;
using HealthyFac.Shared.Domain.Time;
using HealthyFac.Shared.UnitTests.Time.Data;

namespace HealthyFac.Shared.UnitTests.Time;

public sealed class LocalTimesTests
{
    [Theory]
    [MemberData(nameof(LocalTimesTestData.UtcCases), MemberType = typeof(LocalTimesTestData))]
    public void ToUtc_WithDstCase_FollowsCompatibleRule(string local, string utc)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(LocalTimesTestData.ZoneId);
        var localTime = DateTime.ParseExact(local, LocalTimesTestData.LocalFormat, CultureInfo.InvariantCulture);

        var result = LocalTimes.ToUtc(localTime, zone);

        result.Should()
            .Be(
                DateTime.ParseExact(
                    utc,
                    LocalTimesTestData.UtcFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal));
        result.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Theory]
    [MemberData(nameof(LocalTimesTestData.SkippedCases), MemberType = typeof(LocalTimesTestData))]
    public void IsSkipped_WithDstCase_ReturnsSkippedFlag(string local, bool isSkipped)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(LocalTimesTestData.ZoneId);
        var localTime = DateTime.ParseExact(local, LocalTimesTestData.LocalFormat, CultureInfo.InvariantCulture);

        var result = LocalTimes.IsSkipped(localTime, zone);

        result.Should().Be(isSkipped);
    }
}
