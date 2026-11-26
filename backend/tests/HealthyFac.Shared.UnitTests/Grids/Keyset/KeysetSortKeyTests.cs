using HealthyFac.Shared.Application.Grids.Keyset;
using HealthyFac.Shared.UnitTests.Grids.Keyset.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Shared.UnitTests.Grids.Keyset;

[Feature(TestFeatures.Persistence)]
public sealed class KeysetSortKeyTests
{
    [Theory]
    [MemberData(nameof(KeysetSortKeyTestData.UnsupportedKeys), MemberType = typeof(KeysetSortKeyTestData))]
    public void For_WithUnsupportedKey_ThrowsArgumentException(
        string caseName,
        Func<IKeysetSortKey<KeysetSortKeyTestRow>> create
    )
    {
        var act = () => create();

        act.Should().Throw<ArgumentException>(caseName);
    }

    [Fact]
    public void For_WithSupportedKeys_DoesNotThrow()
    {
        var act = () => KeysetSortKeyTestData.CreateSupportedKeys();

        act.Should().NotThrow();
    }
}
