using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.Realtime;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Shifts.Data;
using HealthyFac.Core.IntegrationTests.WorkOrders.Data;
using HealthyFac.Shared.Application.Realtime;

namespace HealthyFac.Core.IntegrationTests.WorkOrders;

[Collection(CoreWorkOrdersCollection.Name)]
public sealed class WorkOrderStatusRealtimeTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task UpdateWorkOrder_WithEstimateChangeOnAnAssignedOrder_RecordsEstimateChangedToTheAssignee()
    {
        var tenant = await ArrangeTenantAsync();
        var workOrderId = await CreateAssignedAsync(tenant, time: "10:00");
        _fixture.RealtimeQueue.Clear();

        await UpdateAsync(tenant, workOrderId, WorkOrderTestData.LongerEstimate, time: "10:00");

        var pushes = PushesOf();
        var estimate = pushes.Should().ContainSingle(push => push.UserId == tenant.IdentityUserId).Subject;
        estimate.Notification!.Kind.Should().Be(RealtimeNotificationKind.WorkOrderEstimateChanged);
        estimate.Notification.Data.Should()
            .BeEquivalentTo(
                new WorkOrderEstimateChangedData
                {
                    WorkOrderId = workOrderId,
                    Title = WorkOrderTestData.ChangedTitle,
                    OldEstimate = TimeSpan.FromMinutes(WorkOrderTestData.OneHour),
                    NewEstimate = TimeSpan.FromMinutes(WorkOrderTestData.LongerEstimate),
                });
        pushes.Should().HaveCount(expected: 2);
    }

    [Fact]
    public async Task UpdateWorkOrder_WithoutEstimateChange_RecordsOnlyTheHint()
    {
        var tenant = await ArrangeTenantAsync();
        var workOrderId = await CreateAssignedAsync(tenant, time: "10:00");
        _fixture.RealtimeQueue.Clear();

        await UpdateAsync(tenant, workOrderId, WorkOrderTestData.OneHour, time: "10:00");

        var push = PushesOf().Should().ContainSingle().Subject;
        push.Group.Should().Be(RealtimeGroups.ForTopic(tenant.CompanyId, RealtimeTopic.WorkOrders));
    }

    [Fact]
    public async Task UpdateWorkOrder_WithEstimateChangeOnABacklogOrder_RecordsOnlyTheHint()
    {
        var tenant = await ArrangeTenantAsync();
        var created = await _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            RandomActor,
            WorkOrderTestData.NewRequest(tenant.CompanyId, AccessScope.Company));
        _fixture.RealtimeQueue.Clear();

        await UpdateAsync(tenant, created.Id, WorkOrderTestData.LongerEstimate);

        var push = PushesOf().Should().ContainSingle().Subject;
        push.Group.Should().Be(RealtimeGroups.ForTopic(tenant.CompanyId, RealtimeTopic.WorkOrders));
    }

    [Fact]
    public async Task ChangeStatus_OfALateCompletion_RecordsRescheduledForMovedOrdersAndTheBacklogReturn()
    {
        var tenant = await ArrangeTenantAsync();
        var finishing = await CreateAssignedAsync(tenant, time: "15:00");
        await CreateAssignedAsync(tenant, time: "16:00");
        await _fixture.ChangeStatusAsync(tenant.CompanyId, RandomActor, finishing, WorkOrderStatus.InProgress);
        _fixture.Clock.Freeze(ShiftTestData.NowOn(ShiftTestData.Tomorrow, time: "16:30"));
        _fixture.RealtimeQueue.Clear();

        await _fixture.ChangeStatusAsync(tenant.CompanyId, RandomActor, finishing, WorkOrderStatus.Completed);

        var pushes = PushesOf();
        var returned = pushes.Should().ContainSingle(push => push.UserId == tenant.IdentityUserId).Subject;
        returned.Notification!.Kind.Should().Be(RealtimeNotificationKind.WorkOrderReturnedToBacklog);
        returned.Notification.Data.Should().BeEquivalentTo(new WorkOrderTitleData { Title = WorkOrderTestData.Title });
        pushes.Should().HaveCount(expected: 3);
    }

    [Fact]
    public async Task ChangeStatus_OfALateCompletionWithAMove_RecordsRescheduledForEachMovedOrder()
    {
        var tenant = await ArrangeTenantAsync();
        var finishing = await CreateAssignedAsync(tenant, time: "10:00");
        var next = await CreateAssignedAsync(tenant, time: "11:00");
        var afternoon = await CreateAssignedAsync(tenant, time: "13:00");
        await _fixture.ChangeStatusAsync(tenant.CompanyId, RandomActor, finishing, WorkOrderStatus.InProgress);
        _fixture.Clock.Freeze(ShiftTestData.NowOn(ShiftTestData.Tomorrow, time: "11:30"));
        _fixture.RealtimeQueue.Clear();

        await _fixture.ChangeStatusAsync(tenant.CompanyId, RandomActor, finishing, WorkOrderStatus.Completed);

        var pushes = PushesOf();
        var rescheduled = pushes.Where(push => push.UserId == tenant.IdentityUserId).ToList();
        rescheduled.Should().HaveCount(expected: 2);
        rescheduled.Should()
            .OnlyContain(push => push.Notification!.Kind == RealtimeNotificationKind.WorkOrderRescheduled);
        rescheduled.Select(push => ((WorkOrderRescheduledData)push.Notification!.Data).WorkOrderId)
            .Should()
            .BeEquivalentTo([next, afternoon]);
        pushes.Should().HaveCount(expected: 5);
    }

    [Fact]
    public async Task ChangeStatus_OfALateCompletionByTheAssignee_RecordsRescheduledForTheAssignee()
    {
        var tenant = await ArrangeTenantAsync();
        var finishing = await CreateAssignedAsync(tenant, time: "10:00");
        var next = await CreateAssignedAsync(tenant, time: "11:00");
        await _fixture.ChangeStatusAsync(
            tenant.CompanyId, tenant.IdentityUserId, finishing, WorkOrderStatus.InProgress);
        _fixture.Clock.Freeze(ShiftTestData.NowOn(ShiftTestData.Tomorrow, time: "11:30"));
        _fixture.RealtimeQueue.Clear();

        await _fixture.ChangeStatusAsync(tenant.CompanyId, tenant.IdentityUserId, finishing, WorkOrderStatus.Completed);

        var rescheduled = PushesOf().Should().ContainSingle(push => push.UserId == tenant.IdentityUserId).Subject;
        rescheduled.Notification!.Kind.Should().Be(RealtimeNotificationKind.WorkOrderRescheduled);
        ((WorkOrderRescheduledData)rescheduled.Notification.Data).WorkOrderId.Should().Be(next);
    }

    [Fact]
    public async Task ChangeStatus_ToBlocked_RecordsBlockedToTheKindGroup()
    {
        var tenant = await ArrangeTenantAsync();
        var workOrderId = await CreateAssignedAsync(tenant, time: "10:00");
        _fixture.RealtimeQueue.Clear();

        await _fixture.ChangeStatusAsync(tenant.CompanyId, RandomActor, workOrderId, WorkOrderStatus.Blocked);

        var pushes = PushesOf();
        var blocked = pushes.Should()
            .ContainSingle(push => push.Group
                == RealtimeGroups.ForKind(
                    tenant.CompanyId,
                    RealtimeNotificationKind.WorkOrderBlocked))
            .Subject;
        blocked.Notification!.Data.Should()
            .BeEquivalentTo(new WorkOrderBlockedData { WorkOrderId = workOrderId, Title = WorkOrderTestData.Title });
        pushes.Should().HaveCount(expected: 2);
    }

    [Fact]
    public async Task ChangeStatus_ToInProgress_RecordsNoBlockedEvent()
    {
        var tenant = await ArrangeTenantAsync();
        var workOrderId = await CreateAssignedAsync(tenant, time: "10:00");
        _fixture.RealtimeQueue.Clear();

        await _fixture.ChangeStatusAsync(tenant.CompanyId, RandomActor, workOrderId, WorkOrderStatus.InProgress);

        var push = PushesOf().Should().ContainSingle().Subject;
        push.Group.Should().Be(RealtimeGroups.ForTopic(tenant.CompanyId, RealtimeTopic.WorkOrders));
    }

    private static Guid RandomActor => Guid.CreateVersion7();

    private List<RealtimePush> PushesOf() => _fixture.RealtimeQueue.Items.Select(item => item.Push).ToList();

    private async Task<ShiftTenant> ArrangeTenantAsync()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        await _fixture.SeedVersionAsync(
            tenant.EmployeeId,
            ShiftTestData.Today.AddDays(value: -30),
            ShiftTestData.Morning,
            ShiftTestData.Afternoon);
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "07:00"));

        return tenant;
    }

    private async Task<Guid> CreateAssignedAsync(ShiftTenant tenant, string time)
    {
        var created = await _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            RandomActor,
            WorkOrderTestData.NewRequest(
                tenant.CompanyId,
                AccessScope.Company,
                assigneeId: tenant.EmployeeId,
                plannedStartLocal: ShiftTestData.LocalStart(ShiftTestData.Tomorrow, time)));

        return created.Id;
    }

    private async Task UpdateAsync(ShiftTenant tenant, Guid workOrderId, int estimateMinutes, string? time = null) =>
        await _fixture.UpdateWorkOrderAsync(
            tenant.CompanyId,
            RandomActor,
            WorkOrderTestData.UpdateRequest(
                tenant.CompanyId,
                AccessScope.Company,
                workOrderId,
                await _fixture.LoadWorkOrderRowVersionAsync(workOrderId),
                [],
                estimateMinutes,
                assigneeId: time is null ? null : tenant.EmployeeId,
                plannedStartLocal: time is null ? null : ShiftTestData.LocalStart(ShiftTestData.Tomorrow, time)));
}
