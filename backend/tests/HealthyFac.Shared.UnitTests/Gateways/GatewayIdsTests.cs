using HealthyFac.Shared.Domain.Gateways;
using HealthyFac.Shared.UnitTests.Gateways.Data;

namespace HealthyFac.Shared.UnitTests.Gateways;

public sealed class GatewayIdsTests
{
    [Fact]
    public void Create_ManyTimes_ProducesValidDistinctIds()
    {
        var ids = Enumerable.Range(start: 0, GatewayIdsTestData.CreatedCount)
            .Select(_ => GatewayIds.Create())
            .ToList();

        ids.Should().OnlyContain(id => id.Length == GatewayIdsTestData.ExpectedLength);
        ids.Should().OnlyContain(id => id.All(character => GatewayIds.Alphabet.Contains(character)));
        ids.Should().OnlyHaveUniqueItems();
    }

    [Theory]
    [MemberData(nameof(GatewayIdsTestData.ValidIds), MemberType = typeof(GatewayIdsTestData))]
    public void IsValid_WithValidId_ReturnsTrue(string? value)
    {
        var isValid = GatewayIds.IsValid(value);

        isValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(GatewayIdsTestData.InvalidIds), MemberType = typeof(GatewayIdsTestData))]
    public void IsValid_WithInvalidId_ReturnsFalse(string? value)
    {
        var isValid = GatewayIds.IsValid(value);

        isValid.Should().BeFalse();
    }
}
