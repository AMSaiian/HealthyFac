using HealthyFac.Core.Domain.Shifts;
using HealthyFac.Core.UnitTests.Shifts.Data;

namespace HealthyFac.Core.UnitTests.Shifts;

public sealed class PastBlocksTests
{
    [Theory]
    [MemberData(nameof(ShiftTestData.ChangedPastCases), MemberType = typeof(ShiftTestData))]
    public void AreUnchanged_WhenPastPartChanges_ReturnsFalse(string before, string after, string now)
    {
        var result = PastBlocks.AreUnchanged(
            ShiftTestData.Blocks(before), ShiftTestData.Blocks(after), ShiftTestData.Now(now));

        result.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(ShiftTestData.KeptPastCases), MemberType = typeof(ShiftTestData))]
    public void AreUnchanged_WhenPastPartStays_ReturnsTrue(string before, string after, string now)
    {
        var result = PastBlocks.AreUnchanged(
            ShiftTestData.Blocks(before), ShiftTestData.Blocks(after), ShiftTestData.Now(now));

        result.Should().BeTrue();
    }
}
