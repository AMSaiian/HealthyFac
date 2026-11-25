using HealthyFac.Seeder.TelemetryHistory;
using HealthyFac.Seeder.UnitTests.TelemetryHistory.Data;

namespace HealthyFac.Seeder.UnitTests.TelemetryHistory;

public sealed class DrainPlannedStartTests
{
    [Fact(Explicit = true)]
    public void Choose_WithAMaintenanceStartInsideABlock_GivesTheMaintenanceStart()
    {
        var start = DrainPlannedStart.Choose(
            DrainPlannedStartTestData.At(day: 2, hour: 10, minute: 0),
            DrainPlannedStartTestData.Estimate,
            DrainPlannedStartTestData.At(day: 1, hour: 20, minute: 0),
            DrainPlannedStartTestData.Blocks,
            []);

        start.Should().Be(DrainPlannedStartTestData.At(day: 2, hour: 10, minute: 0));
    }

    [Fact(Explicit = true)]
    public void Choose_WithAMaintenanceStartOffTheGrid_GivesTheNextGridPoint()
    {
        var start = DrainPlannedStart.Choose(
            DrainPlannedStartTestData.At(day: 2, hour: 9, minute: 2),
            DrainPlannedStartTestData.Estimate,
            DrainPlannedStartTestData.At(day: 1, hour: 20, minute: 0),
            DrainPlannedStartTestData.Blocks,
            []);

        start.Should().Be(DrainPlannedStartTestData.At(day: 2, hour: 9, minute: 5));
    }

    [Fact(Explicit = true)]
    public void Choose_WithAMaintenanceStartAtNight_GivesTheStartOfTheNextBlock()
    {
        var start = DrainPlannedStart.Choose(
            DrainPlannedStartTestData.At(day: 2, hour: 2, minute: 0),
            DrainPlannedStartTestData.Estimate,
            DrainPlannedStartTestData.At(day: 1, hour: 20, minute: 0),
            DrainPlannedStartTestData.Blocks,
            []);

        start.Should().Be(DrainPlannedStartTestData.At(day: 2, hour: 8, minute: 0));
    }

    [Fact(Explicit = true)]
    public void Choose_WithABlockWhoseRestIsShorterThanTheEstimate_GivesTheStartOfTheBlockAfterIt()
    {
        var start = DrainPlannedStart.Choose(
            DrainPlannedStartTestData.At(day: 2, hour: 11, minute: 30),
            DrainPlannedStartTestData.Estimate,
            DrainPlannedStartTestData.At(day: 1, hour: 20, minute: 0),
            DrainPlannedStartTestData.Blocks,
            []);

        start.Should().Be(DrainPlannedStartTestData.At(day: 2, hour: 13, minute: 0));
    }

    [Fact(Explicit = true)]
    public void Choose_WithABlockThatBeginsBeforeTheAssignment_SkipsIt()
    {
        var start = DrainPlannedStart.Choose(
            DrainPlannedStartTestData.At(day: 2, hour: 12, minute: 30),
            DrainPlannedStartTestData.Estimate,
            DrainPlannedStartTestData.At(day: 2, hour: 9, minute: 0),
            DrainPlannedStartTestData.Blocks,
            []);

        start.Should().Be(DrainPlannedStartTestData.At(day: 2, hour: 13, minute: 0));
    }

    [Fact(Explicit = true)]
    public void Choose_WithNoBlockThatHoldsTheEstimate_GivesNull()
    {
        var start = DrainPlannedStart.Choose(
            DrainPlannedStartTestData.At(day: 2, hour: 2, minute: 0),
            TimeSpan.FromHours(hours: 9),
            DrainPlannedStartTestData.At(day: 1, hour: 20, minute: 0),
            DrainPlannedStartTestData.Blocks,
            []);

        start.Should().BeNull();
    }

    [Fact(Explicit = true)]
    public void Choose_WithABusySlotOnTheMaintenanceStart_GivesTheFirstFreeGridMomentAfterIt()
    {
        var start = DrainPlannedStart.Choose(
            DrainPlannedStartTestData.At(day: 2, hour: 9, minute: 0),
            TimeSpan.FromHours(hours: 1),
            DrainPlannedStartTestData.At(day: 1, hour: 20, minute: 0),
            DrainPlannedStartTestData.Blocks,
            [DrainPlannedStartTestData.Busy(day: 2, startHour: 8, endHour: 10)]);

        start.Should().Be(DrainPlannedStartTestData.At(day: 2, hour: 10, minute: 0));
    }

    [Fact(Explicit = true)]
    public void Choose_WithABusyBlockStart_GivesTheMomentAfterTheBusySlot()
    {
        var start = DrainPlannedStart.Choose(
            DrainPlannedStartTestData.At(day: 2, hour: 2, minute: 0),
            TimeSpan.FromHours(hours: 1),
            DrainPlannedStartTestData.At(day: 1, hour: 20, minute: 0),
            DrainPlannedStartTestData.Blocks,
            [DrainPlannedStartTestData.Busy(day: 2, startHour: 8, endHour: 9)]);

        start.Should().Be(DrainPlannedStartTestData.At(day: 2, hour: 9, minute: 0));
    }

    [Fact(Explicit = true)]
    public void Choose_WithABusySlotThatFillsTheRestOfTheBlock_GivesTheNextBlock()
    {
        var start = DrainPlannedStart.Choose(
            DrainPlannedStartTestData.At(day: 2, hour: 9, minute: 0),
            TimeSpan.FromHours(hours: 1),
            DrainPlannedStartTestData.At(day: 1, hour: 20, minute: 0),
            DrainPlannedStartTestData.Blocks,
            [DrainPlannedStartTestData.Busy(day: 2, startHour: 8, endHour: 12)]);

        start.Should().Be(DrainPlannedStartTestData.At(day: 2, hour: 13, minute: 0));
    }
}
