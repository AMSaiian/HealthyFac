using HealthyFac.Core.Domain.WorkOrders.Scheduling;
using HealthyFac.Core.UnitTests.WorkOrders.Scheduling.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Core.UnitTests.WorkOrders.Scheduling;

[Feature(TestFeatures.WorkOrders)]
public sealed class SlotRulesTests
{
    [Theory]
    [MemberData(nameof(SlotRulesTestData.SlotsInsideOneBlock), MemberType = typeof(SlotRulesTestData))]
    public void FitsOneBlock_WithSlotInsideOneBlock_ReturnsTrue(string caseName, string start, int minutes)
    {
        var result = SlotRules.FitsOneBlock(
            SchedulingTestData.Slot(number: 1, start, minutes), SchedulingTestData.Day());

        result.Should().BeTrue(because: caseName);
    }

    [Fact]
    public void FitsOneBlock_WithSlotAcrossLunch_ReturnsFalse()
    {
        var result = SlotRules.FitsOneBlock(
            SchedulingTestData.Slot(number: 1, start: "11:30", minutes: 120), SchedulingTestData.Day());

        result.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(SlotRulesTestData.SlotsThatDoNotOverlap), MemberType = typeof(SlotRulesTestData))]
    public void Overlaps_WithSlotThatDoesNotOverlap_ReturnsFalse(
        string caseName,
        int otherNumber,
        string otherStart,
        string start
    )
    {
        var other = SchedulingTestData.Slot(otherNumber, otherStart, minutes: 60);

        var result = SlotRules.Overlaps(SchedulingTestData.Slot(number: 1, start, minutes: 60), [other]);

        result.Should().BeFalse(caseName);
    }

    [Fact]
    public void Overlaps_WithSharedMinutes_ReturnsTrue()
    {
        var other = SchedulingTestData.Slot(number: 2, start: "09:00", minutes: 60);

        var result = SlotRules.Overlaps(SchedulingTestData.Slot(number: 1, start: "09:55", minutes: 60), [other]);

        result.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(SlotRulesTestData.Completions), MemberType = typeof(SlotRulesTestData))]
    public void Of_WithCompletion_EndsAtTheLaterOfPlannedEndAndCompletion(
        string caseName,
        string completedAt,
        string expectedEnd
    )
    {
        var occupancy = Occupancy.Of(
            SchedulingTestData.Id(number: 1),
            SchedulingTestData.At(time: "09:00"),
            TimeSpan.FromHours(value: 1),
            SchedulingTestData.At(completedAt));

        occupancy.EndUtc.Should().Be(SchedulingTestData.At(expectedEnd), caseName);
    }
}
