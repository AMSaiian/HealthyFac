using HealthyFac.Core.Domain.Shifts;
using HealthyFac.Core.Domain.Shifts.DaySchedules;
using HealthyFac.Core.UnitTests.Shifts.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Core.UnitTests.Shifts;

[Feature(TestFeatures.Shifts)]
public sealed class BlockJoinTests
{
    [Theory]
    [MemberData(nameof(ShiftTestData.JoinCases), MemberType = typeof(ShiftTestData))]
    public void Join_ForEachCase_JoinsOnlyBlocksThatTouch(string caseName, string blocks, string expected)
    {
        var result = BlockJoin.Join(Parse(blocks));

        result.Should().Equal(Parse(expected), caseName);
    }

    [Fact]
    public void JoinPerWeekday_ForTouchingBlocksOfOneDay_JoinsThemAndKeepsTheOtherDay()
    {
        var result = BlockJoin.JoinPerWeekday(
        [
            ShiftTestData.Template(Weekday.Monday, start: "08:00", end: "12:00"),
            ShiftTestData.Template(Weekday.Tuesday, start: "12:00", end: "13:00"),
            ShiftTestData.Template(Weekday.Monday, start: "12:00", end: "13:00"),
        ]);

        result.Should()
            .Equal(
                ShiftTestData.Template(Weekday.Monday, start: "08:00", end: "13:00"),
                ShiftTestData.Template(Weekday.Tuesday, start: "12:00", end: "13:00"));
    }

    private static List<BlockTimes> Parse(string blocks) =>
    [
        .. ShiftTestData.Blocks(blocks)
            .Select(block => new BlockTimes
            {
                Start = block.Start,
                End = block.End,
            }),
    ];
}
