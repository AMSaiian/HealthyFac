using HealthyFac.Shared.UnitTests.Grids.Keyset.Data;
using HealthyFac.Testing.Conventions;

namespace HealthyFac.Shared.UnitTests.Grids.Keyset;

public sealed class KeysetSortMapConventionTests
{
    [Theory]
    [MemberData(nameof(KeysetSortMapTestData.Cases), MemberType = typeof(KeysetSortMapTestData))]
    public void KeysetSortMapsUseSupportedKeys_WithMapType_ReportsOnlyUnsupportedMaps(
        string caseName,
        Type mapType,
        int expectedViolationCount
    )
    {
        var violations = ConventionChecks.KeysetSortMapsUseSupportedKeys([mapType]);

        violations.Should().HaveCount(expectedViolationCount, caseName);
    }
}
