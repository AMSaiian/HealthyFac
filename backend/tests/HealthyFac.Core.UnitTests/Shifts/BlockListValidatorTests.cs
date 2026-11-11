using HealthyFac.Core.Application.Shifts;
using HealthyFac.Core.UnitTests.Shifts.Data;

namespace HealthyFac.Core.UnitTests.Shifts;

public sealed class BlockListValidatorTests
{
    private readonly BlockListValidator _validator = new();

    [Theory]
    [MemberData(nameof(BlockListTestData.InvalidLists), MemberType = typeof(BlockListTestData))]
    public void Validate_WithBrokenRule_Fails(string[] ranges)
    {
        var result = _validator.Validate(BlockListTestData.Blocks(ranges));

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(BlockListTestData.ValidLists), MemberType = typeof(BlockListTestData))]
    public void Validate_WithValidList_Passes(string[] ranges)
    {
        var result = _validator.Validate(BlockListTestData.Blocks(ranges));

        result.IsValid.Should().BeTrue();
    }
}
