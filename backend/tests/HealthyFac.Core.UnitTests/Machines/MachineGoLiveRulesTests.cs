using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.UnitTests.Machines.Data;

namespace HealthyFac.Core.UnitTests.Machines;

public sealed class MachineGoLiveRulesTests
{
    [Theory]
    [MemberData(nameof(MachineGoLiveRulesTestData.StartsOfDay), MemberType = typeof(MachineGoLiveRulesTestData))]
    public void StartOfDayUtc_WithDayAndZone_ReturnsTheUtcInstantOfLocalMidnight(
        string caseName,
        DateOnly day,
        string zoneId,
        DateTime expected
    )
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);

        var startsAt = MachineGoLiveRules.StartOfDayUtc(day, zone);

        startsAt.Should().Be(expected, caseName);
        startsAt.Kind.Should().Be(DateTimeKind.Utc);
    }
}
