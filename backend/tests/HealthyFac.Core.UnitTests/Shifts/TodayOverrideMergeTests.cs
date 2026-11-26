using HealthyFac.Core.Domain.Shifts.DaySchedules;
using HealthyFac.Core.UnitTests.Shifts.Data;

namespace HealthyFac.Core.UnitTests.Shifts;

public sealed class TodayOverrideMergeTests
{
    [Theory]
    [MemberData(nameof(TodayOverrideMergeTestData.Cases), MemberType = typeof(TodayOverrideMergeTestData))]
    public void Merge_KeepsPastOverrideAndReturnsRestToTemplate(
        string caseName,
        string overrideBlocks,
        string templateBlocks,
        string now,
        string nowOnGrid,
        string expected
    )
    {
        var result = TodayOverrideMerge.Merge(
            ShiftTestData.Blocks(overrideBlocks),
            ShiftTestData.Blocks(templateBlocks),
            ShiftTestData.Now(now),
            ShiftTestData.Now(nowOnGrid));

        result.Select(block => block.StartUtc)
            .Should()
            .Equal(ShiftTestData.Blocks(expected).Select(block => block.StartUtc), caseName);

        result.Select(block => block.EndUtc)
            .Should()
            .Equal(ShiftTestData.Blocks(expected).Select(block => block.EndUtc), caseName);
    }

    [Fact]
    public void Merge_FillsLocalTimesOfCutBlocks()
    {
        var result = TodayOverrideMerge.Merge(
            ShiftTestData.Blocks(blocks: ""), ShiftTestData.Blocks(blocks: "13:00-17:00"),
            ShiftTestData.Now(time: "14:02"),
            ShiftTestData.Now(time: "14:05"));

        result.Should()
            .ContainSingle()
            .Which.Should()
            .Match<DayBlock>(block => block.Start == new TimeOnly(14, 5) && block.End == new TimeOnly(17, 0));
    }
}
