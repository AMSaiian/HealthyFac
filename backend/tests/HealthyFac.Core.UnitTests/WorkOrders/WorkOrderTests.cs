using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.UnitTests.WorkOrders.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Core.UnitTests.WorkOrders;

[Feature(TestFeatures.WorkOrders)]
public sealed class WorkOrderTests
{
    [Fact]
    public void Create_Always_AddsBacklogRevisionOne()
    {
        var workOrder = WorkOrderTestData.CreateBacklog();

        workOrder.CurrentRevision.Should().Be(expected: 1);

        workOrder.Current.Should()
            .BeEquivalentTo(
                new
                {
                    Revision = 1,
                    Status = WorkOrderStatus.Backlog,
                    AssigneeId = (Guid?)null,
                    WorkOrderId = workOrder.Id,
                });

        workOrder.Current.PlannedStartLocal.Should().BeNull();
        workOrder.Current.PlannedStartUtc.Should().BeNull();
    }

    [Fact]
    public void Assign_FromBacklog_AddsStagedRowAsNewest()
    {
        var workOrder = WorkOrderTestData.CreateBacklog();

        workOrder.Assign(WorkOrderTestData.AssigneeId, WorkOrderTestData.StartLocal, WorkOrderTestData.StartUtc);

        AssertCurrentIsNewest(workOrder, expectedRevision: 2);
        workOrder.Current.Status.Should().Be(WorkOrderStatus.Staged);
        workOrder.Current.AssigneeId.Should().Be(WorkOrderTestData.AssigneeId);
        workOrder.Current.PlannedStartLocal.Should().Be(WorkOrderTestData.StartLocal);
        workOrder.Current.PlannedStartLocal.Value.Kind.Should().Be(DateTimeKind.Unspecified);
        workOrder.Current.PlannedStartUtc.Should().Be(WorkOrderTestData.StartUtc);
        workOrder.Current.PlannedStartUtc.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Move_FromScheduledStatus_KeepsStatusAndChangesAssigneeAndStart()
    {
        var workOrder = WorkOrderTestData.CreateInProgress();

        workOrder.Move(
            WorkOrderTestData.OtherAssigneeId, WorkOrderTestData.LaterStartLocal, WorkOrderTestData.LaterStartUtc);

        AssertCurrentIsNewest(workOrder, expectedRevision: 4);
        workOrder.Current.Status.Should().Be(WorkOrderStatus.InProgress);
        workOrder.Current.AssigneeId.Should().Be(WorkOrderTestData.OtherAssigneeId);
        workOrder.Current.PlannedStartUtc.Should().Be(WorkOrderTestData.LaterStartUtc);
    }

    [Fact]
    public void Unassign_FromScheduledStatus_AddsBacklogRowWithoutAssigneeAndStart()
    {
        var workOrder = WorkOrderTestData.CreateInProgress();

        workOrder.Unassign();

        AssertCurrentIsNewest(workOrder, expectedRevision: 4);
        workOrder.Current.Status.Should().Be(WorkOrderStatus.Backlog);
        workOrder.Current.AssigneeId.Should().BeNull();
        workOrder.Current.PlannedStartLocal.Should().BeNull();
        workOrder.Current.PlannedStartUtc.Should().BeNull();
    }

    [Fact]
    public void ChangeStatus_FromScheduledStatus_CopiesAssigneeAndStart()
    {
        var workOrder = WorkOrderTestData.CreateStaged();

        workOrder.ChangeStatus(WorkOrderStatus.Blocked);

        AssertCurrentIsNewest(workOrder, expectedRevision: 3);
        workOrder.Current.Status.Should().Be(WorkOrderStatus.Blocked);
        workOrder.Current.AssigneeId.Should().Be(WorkOrderTestData.AssigneeId);
        workOrder.Current.PlannedStartLocal.Should().Be(WorkOrderTestData.StartLocal);
        workOrder.Current.PlannedStartUtc.Should().Be(WorkOrderTestData.StartUtc);
    }

    [Fact]
    public void Assign_FromScheduledStatus_ThrowsAndAddsNoRow()
    {
        var workOrder = WorkOrderTestData.CreateStaged();

        var act = () => workOrder.Assign(
            WorkOrderTestData.OtherAssigneeId, WorkOrderTestData.LaterStartLocal, WorkOrderTestData.LaterStartUtc);

        act.Should().Throw<InvalidOperationException>();
        AssertCurrentIsNewest(workOrder, expectedRevision: 2);
    }

    [Fact]
    public void Move_FromBacklog_ThrowsAndAddsNoRow()
    {
        var workOrder = WorkOrderTestData.CreateBacklog();

        var act = () => workOrder.Move(
            WorkOrderTestData.AssigneeId, WorkOrderTestData.StartLocal, WorkOrderTestData.StartUtc);

        act.Should().Throw<InvalidOperationException>();
        AssertCurrentIsNewest(workOrder, expectedRevision: 1);
    }

    [Fact]
    public void Unassign_FromBacklog_ThrowsAndAddsNoRow()
    {
        var workOrder = WorkOrderTestData.CreateBacklog();

        var act = workOrder.Unassign;

        act.Should().Throw<InvalidOperationException>();
        AssertCurrentIsNewest(workOrder, expectedRevision: 1);
    }

    [Theory]
    [MemberData(
        nameof(WorkOrderTestData.StatusesThatBreakTheRuleOnStaged),
        MemberType = typeof(WorkOrderTestData)
    )]
    public void ChangeStatus_ToStatusThatBreaksTheRule_ThrowsAndAddsNoRow(
        string caseName,
        WorkOrderStatus status
    )
    {
        var workOrder = WorkOrderTestData.CreateStaged();

        var act = () => workOrder.ChangeStatus(status);

        act.Should().Throw<InvalidOperationException>(caseName);
        AssertCurrentIsNewest(workOrder, expectedRevision: 2);
    }

    [Fact]
    public void ChangeStatus_FromBacklog_ThrowsAndAddsNoRow()
    {
        var workOrder = WorkOrderTestData.CreateBacklog();

        var act = () => workOrder.ChangeStatus(WorkOrderStatus.InProgress);

        act.Should().Throw<InvalidOperationException>();
        AssertCurrentIsNewest(workOrder, expectedRevision: 1);
    }

    [Fact]
    public void ReplaceFiles_WithAddedAndRemovedIds_AddsNewLinksAndReturnsRemovedOnes()
    {
        var workOrder = WorkOrderTestData.CreateBacklog();
        workOrder.ReplaceFiles([WorkOrderTestData.FileA, WorkOrderTestData.FileB]);

        var removed = workOrder.ReplaceFiles([WorkOrderTestData.FileB, WorkOrderTestData.FileC]);

        removed.Should().ContainSingle().Which.FileId.Should().Be(WorkOrderTestData.FileA);

        workOrder.Files.Select(link => link.FileId)
            .Should()
            .BeEquivalentTo([WorkOrderTestData.FileB, WorkOrderTestData.FileC]);
    }

    [Fact]
    public void ReplaceFiles_WithSameList_ChangesNothing()
    {
        var workOrder = WorkOrderTestData.CreateBacklog();
        workOrder.ReplaceFiles([WorkOrderTestData.FileA, WorkOrderTestData.FileB]);
        var linksBefore = workOrder.Files.ToList();

        var removed = workOrder.ReplaceFiles([WorkOrderTestData.FileB, WorkOrderTestData.FileA]);

        removed.Should().BeEmpty();
        workOrder.Files.Should().Equal(linksBefore);
    }

    [Fact]
    public void Create_Always_StoresTheEstimateInTheFirstRow()
    {
        var workOrder = WorkOrderTestData.CreateBacklog();

        AssertCurrentIsNewest(workOrder, expectedRevision: 1);
        workOrder.Current.Estimate.Should().Be(TimeSpan.FromHours(value: 1));
    }

    [Fact]
    public void ChangeEstimate_WithAnotherValue_AddsRowWithCurrentStatusAssigneeAndStart()
    {
        var workOrder = WorkOrderTestData.CreateInProgress();

        workOrder.ChangeEstimate(TimeSpan.FromHours(value: 2));

        AssertCurrentIsNewest(workOrder, expectedRevision: 4);
        workOrder.Current.Status.Should().Be(WorkOrderStatus.InProgress);
        workOrder.Current.AssigneeId.Should().Be(WorkOrderTestData.AssigneeId);
        workOrder.Current.PlannedStartUtc.Should().Be(WorkOrderTestData.StartUtc);
        workOrder.Current.Estimate.Should().Be(TimeSpan.FromHours(value: 2));
    }

    [Fact]
    public void ChangeEstimate_WithTheSameValue_AddsNoRow()
    {
        var workOrder = WorkOrderTestData.CreateStaged();

        workOrder.ChangeEstimate(TimeSpan.FromHours(value: 1));

        AssertCurrentIsNewest(workOrder, expectedRevision: 2);
    }

    [Theory]
    [MemberData(nameof(WorkOrderTestData.RescheduleCases), MemberType = typeof(WorkOrderTestData))]
    public void Reschedule_WithEachCase_AddsAtMostOneRowWithAllNewValues(
        string caseName,
        WorkOrder workOrder,
        WorkOrderSlot? slot,
        TimeSpan estimate,
        WorkOrderScheduleChange expectedChange,
        int expectedRevision
    )
    {
        var change = workOrder.Reschedule(slot, estimate);

        change.Should().Be(expectedChange, caseName);
        AssertCurrentIsNewest(workOrder, expectedRevision);
        workOrder.Current.Estimate.Should().Be(estimate, caseName);
    }

    [Fact]
    public void Reschedule_WithNewStartAndNewEstimate_AddsOneRowWithBoth()
    {
        var workOrder = WorkOrderTestData.CreateStaged();

        workOrder.Reschedule(WorkOrderTestData.LaterSlot, TimeSpan.FromHours(value: 3));

        AssertCurrentIsNewest(workOrder, expectedRevision: 3);
        workOrder.Current.PlannedStartUtc.Should().Be(WorkOrderTestData.LaterStartUtc);
        workOrder.Current.Estimate.Should().Be(TimeSpan.FromHours(value: 3));
    }

    private static void AssertCurrentIsNewest(WorkOrder workOrder, int expectedRevision)
    {
        workOrder.CurrentRevision.Should().Be(expectedRevision);
        workOrder.History.Max(row => row.Revision).Should().Be(expectedRevision);
        workOrder.History.Should().HaveCount(expectedRevision);
        workOrder.Current.Revision.Should().Be(expectedRevision);
        workOrder.Current.Estimate.Should().Be(workOrder.Estimate);
    }
}
