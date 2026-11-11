using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.Domain.WorkOrders.Scheduling;
using HealthyFac.Core.UnitTests.WorkOrders.Scheduling.Data;

namespace HealthyFac.Core.UnitTests.WorkOrders.Scheduling;

public sealed class CompletionSlideTests
{
    private static IReadOnlyList<SlideMove> Plan(
        SlideWorkOrder completed,
        string completedAt,
        params SlideWorkOrder[] scheduled
    ) =>
        CompletionSlide.Plan(
            completed,
            SchedulingTestData.At(completedAt),
            scheduled,
            SchedulingTestData.Day(),
            SchedulingTestData.Zone);

    private static SlideWorkOrder Completed() =>
        SchedulingTestData.Order(number: 1, WorkOrderStatus.InProgress, start: "08:00", minutes: 60);

    [Fact]
    public void Plan_WithOnTimeCompletion_MovesNothing()
    {
        var next = SchedulingTestData.Order(number: 2, WorkOrderStatus.Staged, start: "09:00", minutes: 60);

        var moves = Plan(Completed(), completedAt: "09:00", next);

        moves.Should().BeEmpty();
    }

    [Fact]
    public void Plan_WithEarlyCompletion_MovesNothing()
    {
        var next = SchedulingTestData.Order(number: 2, WorkOrderStatus.Staged, start: "09:00", minutes: 60);

        var moves = Plan(Completed(), completedAt: "08:30", next);

        moves.Should().BeEmpty();
    }

    [Fact]
    public void Plan_WithLateCompletion_PushesTheNextOneToTheCompletionMoment()
    {
        var next = SchedulingTestData.Order(number: 2, WorkOrderStatus.Staged, start: "09:00", minutes: 60);

        var moves = Plan(Completed(), completedAt: "09:30", next);

        moves.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new { WorkOrderId = next.Id, NewStartUtc = (DateTime?)SchedulingTestData.At(time: "09:30") });
    }

    [Fact]
    public void Plan_WithLateCompletion_RoundsTheNewStartUpToTheGrid()
    {
        var next = SchedulingTestData.Order(number: 2, WorkOrderStatus.Staged, start: "09:00", minutes: 60);

        var moves = Plan(Completed(), completedAt: "09:32", next);

        moves.Single().NewStartUtc.Should().Be(SchedulingTestData.At(time: "09:35"));
    }

    [Fact]
    public void Plan_WhenPushReachesLunch_JumpsIntoTheAfternoonBlock()
    {
        var completed = SchedulingTestData.Order(number: 1, WorkOrderStatus.InProgress, start: "10:00", minutes: 60);
        var next = SchedulingTestData.Order(number: 2, WorkOrderStatus.Staged, start: "11:00", minutes: 60);

        var moves = Plan(completed, completedAt: "11:30", next);

        moves.Single().NewStartUtc.Should().Be(SchedulingTestData.At(time: "13:00"));
    }

    [Fact]
    public void Plan_WithFixedWorkOrderAhead_PlacesAroundIt()
    {
        var fixedOrder = SchedulingTestData.Order(number: 3, WorkOrderStatus.InProgress, start: "10:00", minutes: 60);
        var next = SchedulingTestData.Order(number: 2, WorkOrderStatus.Staged, start: "09:00", minutes: 60);

        var moves = Plan(Completed(), completedAt: "09:30", next, fixedOrder);

        moves.Should().ContainSingle().Which.NewStartUtc.Should().Be(SchedulingTestData.At(time: "11:00"));
    }

    [Fact]
    public void Plan_WhenNothingFits_SendsTheWorkOrderToTheBacklog()
    {
        var next = SchedulingTestData.Order(number: 2, WorkOrderStatus.Blocked, start: "09:00", minutes: 300);

        var moves = Plan(Completed(), completedAt: "09:30", next);

        moves.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new { WorkOrderId = next.Id, NewStartUtc = (DateTime?)null });
    }

    [Fact]
    public void Plan_WhenOwnStartStillFits_ReportsNoMoveAndPushesTheFollowingOne()
    {
        var first = SchedulingTestData.Order(number: 2, WorkOrderStatus.Staged, start: "09:15", minutes: 60);
        var second = SchedulingTestData.Order(number: 3, WorkOrderStatus.Rejected, start: "10:00", minutes: 60);

        var moves = Plan(Completed(), completedAt: "09:10", first, second);

        moves.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new { WorkOrderId = second.Id, NewStartUtc = (DateTime?)SchedulingTestData.At(time: "10:15") });
    }

    [Fact]
    public void Plan_WithCompletionOnTheNextDay_SendsEveryMovableOneToTheBacklog()
    {
        var completed = SchedulingTestData.Order(number: 1, WorkOrderStatus.InProgress, start: "16:00", minutes: 60);
        var staged = SchedulingTestData.Order(number: 2, WorkOrderStatus.Staged, start: "16:30", minutes: 30);
        var fixedOrder = SchedulingTestData.Order(number: 3, WorkOrderStatus.InProgress, start: "16:45", minutes: 15);

        var moves = CompletionSlide.Plan(
            completed,
            SchedulingTestData.NextDayAt(time: "09:00"),
            [staged, fixedOrder],
            SchedulingTestData.Day(),
            SchedulingTestData.Zone);

        moves.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new { WorkOrderId = staged.Id, NewStartUtc = (DateTime?)null });
    }

    [Fact]
    public void Plan_WithWorkOrderBeforeTheCompletedOne_LeavesItAlone()
    {
        var earlier = SchedulingTestData.Order(number: 2, WorkOrderStatus.Staged, start: "08:00", minutes: 60);
        var completed = SchedulingTestData.Order(number: 1, WorkOrderStatus.InProgress, start: "09:00", minutes: 60);

        var moves = Plan(completed, completedAt: "10:30", earlier);

        moves.Should().BeEmpty();
    }
}
