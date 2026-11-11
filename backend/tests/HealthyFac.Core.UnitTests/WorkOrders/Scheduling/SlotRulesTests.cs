using HealthyFac.Core.Domain.WorkOrders.Scheduling;
using HealthyFac.Core.UnitTests.WorkOrders.Scheduling.Data;

namespace HealthyFac.Core.UnitTests.WorkOrders.Scheduling;

public sealed class SlotRulesTests
{
    [Fact]
    public void FitsOneBlock_WithSlotInsideBlock_ReturnsTrue()
    {
        var result = SlotRules.FitsOneBlock(
            SchedulingTestData.Slot(number: 1, start: "09:00", minutes: 60), SchedulingTestData.Day());

        result.Should().BeTrue();
    }

    [Fact]
    public void FitsOneBlock_WithSlotUpToBlockEnd_ReturnsTrue()
    {
        var result = SlotRules.FitsOneBlock(
            SchedulingTestData.Slot(number: 1, start: "11:00", minutes: 60), SchedulingTestData.Day());

        result.Should().BeTrue();
    }

    [Fact]
    public void FitsOneBlock_WithSlotAcrossLunch_ReturnsFalse()
    {
        var result = SlotRules.FitsOneBlock(
            SchedulingTestData.Slot(number: 1, start: "11:30", minutes: 120), SchedulingTestData.Day());

        result.Should().BeFalse();
    }

    [Fact]
    public void Overlaps_WithBackToBackSlots_ReturnsFalse()
    {
        var other = SchedulingTestData.Slot(number: 2, start: "09:00", minutes: 60);

        var result = SlotRules.Overlaps(SchedulingTestData.Slot(number: 1, start: "10:00", minutes: 60), [other]);

        result.Should().BeFalse();
    }

    [Fact]
    public void Overlaps_WithSharedMinutes_ReturnsTrue()
    {
        var other = SchedulingTestData.Slot(number: 2, start: "09:00", minutes: 60);

        var result = SlotRules.Overlaps(SchedulingTestData.Slot(number: 1, start: "09:55", minutes: 60), [other]);

        result.Should().BeTrue();
    }

    [Fact]
    public void Overlaps_WithOwnOccupancy_ReturnsFalse()
    {
        var own = SchedulingTestData.Slot(number: 1, start: "09:00", minutes: 60);

        var result = SlotRules.Overlaps(SchedulingTestData.Slot(number: 1, start: "09:30", minutes: 60), [own]);

        result.Should().BeFalse();
    }

    [Fact]
    public void Of_WithLateCompletion_BlocksUntilCompletion()
    {
        var occupancy = Occupancy.Of(
            SchedulingTestData.Id(number: 1),
            SchedulingTestData.At(time: "09:00"),
            TimeSpan.FromHours(value: 1),
            SchedulingTestData.At(time: "10:40"));

        occupancy.EndUtc.Should().Be(SchedulingTestData.At(time: "10:40"));
    }

    [Fact]
    public void Of_WithEarlyCompletion_KeepsPlannedEnd()
    {
        var occupancy = Occupancy.Of(
            SchedulingTestData.Id(number: 1),
            SchedulingTestData.At(time: "09:00"),
            TimeSpan.FromHours(value: 1),
            SchedulingTestData.At(time: "09:20"));

        occupancy.EndUtc.Should().Be(SchedulingTestData.At(time: "10:00"));
    }
}
