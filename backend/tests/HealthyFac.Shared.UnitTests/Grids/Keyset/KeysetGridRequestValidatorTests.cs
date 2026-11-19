using HealthyFac.Shared.UnitTests.Grids.Data;
using HealthyFac.Shared.UnitTests.Grids.Keyset.Data;

namespace HealthyFac.Shared.UnitTests.Grids.Keyset;

public sealed class KeysetGridRequestValidatorTests
{
    [Fact]
    public void Validate_WithoutFilter_IsValid()
    {
        var validator = new TestKeysetGridRequestValidator(TestSortColumn.All);

        var result = validator.Validate(KeysetGridRequestTestData.CreateRequestWithoutFilter());

        result.IsValid.Should().BeTrue();
    }
}
