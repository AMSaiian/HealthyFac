using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.Realtime;
using HealthyFac.Core.Application.WorkOrders;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Core.IntegrationTests.Shifts.Data;
using HealthyFac.Core.IntegrationTests.WorkOrders.Data;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.WorkOrders;

[Collection(CoreWorkOrdersCollection.Name)]
public sealed class WorkOrderAssignmentRealtimeTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task AssignWorkOrder_WithValidSlot_RecordsAssignedToTheAssigneeAndTheHint()
    {
        var tenant = await ArrangeTenantAsync();
        var workOrderId = await CreateBacklogAsync(tenant);
        _fixture.RealtimeQueue.Clear();

        await AssignAsync(tenant, workOrderId, tenant.EmployeeId, WorkOrderTestData.ScheduledTime);

        var pushes = PushesOf();
        var assigned = pushes.Should().ContainSingle(push => push.UserId == tenant.IdentityUserId).Subject;
        assigned.Notification!.Kind.Should().Be(RealtimeNotificationKind.WorkOrderAssigned);
        assigned.Notification.Data.Should()
            .BeEquivalentTo(
                new WorkOrderAssignedData
                {
                    WorkOrderId = workOrderId,
                    Title = WorkOrderTestData.Title,
                    PlannedStartLocal = ShiftTestData.LocalStart(
                        ShiftTestData.Tomorrow, WorkOrderTestData.ScheduledTime),
                });
        HintOf(pushes, tenant).Should().NotBeNull();
        pushes.Should().HaveCount(expected: 2);
    }

    [Fact]
    public async Task UnassignWorkOrder_OfScheduledWorkOrder_RecordsUnassignedToTheOldAssignee()
    {
        var tenant = await ArrangeTenantAsync();
        var workOrderId = await CreateBacklogAsync(tenant);
        await AssignAsync(tenant, workOrderId, tenant.EmployeeId, WorkOrderTestData.ScheduledTime);
        _fixture.RealtimeQueue.Clear();

        await UnassignAsync(tenant, workOrderId);

        var pushes = PushesOf();
        var unassigned = pushes.Should().ContainSingle(push => push.UserId == tenant.IdentityUserId).Subject;
        unassigned.Notification!.Kind.Should().Be(RealtimeNotificationKind.WorkOrderUnassigned);
        unassigned.Notification.Data.Should()
            .BeEquivalentTo(new WorkOrderTitleData { Title = WorkOrderTestData.Title });
        HintOf(pushes, tenant).Should().NotBeNull();
        pushes.Should().HaveCount(expected: 2);
    }

    [Fact]
    public async Task MoveWorkOrder_ToAnotherAssignee_RecordsUnassignedToTheOldAndAssignedToTheNew()
    {
        var (tenant, other) = await ArrangeTwoAssigneesAsync();
        var workOrderId = await CreateBacklogAsync(tenant);
        await AssignAsync(tenant, workOrderId, tenant.EmployeeId, WorkOrderTestData.ScheduledTime);
        _fixture.RealtimeQueue.Clear();

        await MoveAsync(tenant, workOrderId, other.EmployeeId, WorkOrderTestData.NextTime);

        var pushes = PushesOf();
        pushes.Should().HaveCount(expected: 3);
        KindFor(pushes, tenant.IdentityUserId).Should().Be(RealtimeNotificationKind.WorkOrderUnassigned);
        KindFor(pushes, other.IdentityUserId).Should().Be(RealtimeNotificationKind.WorkOrderAssigned);
    }

    [Fact]
    public async Task MoveWorkOrder_InTheSameAssignee_RecordsRescheduled()
    {
        var tenant = await ArrangeTenantAsync();
        var workOrderId = await CreateBacklogAsync(tenant);
        await AssignAsync(tenant, workOrderId, tenant.EmployeeId, WorkOrderTestData.ScheduledTime);
        _fixture.RealtimeQueue.Clear();

        await MoveAsync(tenant, workOrderId, tenant.EmployeeId, WorkOrderTestData.NextTime);

        var pushes = PushesOf();
        var rescheduled = pushes.Should().ContainSingle(push => push.UserId == tenant.IdentityUserId).Subject;
        rescheduled.Notification!.Kind.Should().Be(RealtimeNotificationKind.WorkOrderRescheduled);
        rescheduled.Notification.Data.Should()
            .BeEquivalentTo(
                new WorkOrderRescheduledData
                {
                    WorkOrderId = workOrderId,
                    Title = WorkOrderTestData.Title,
                    PlannedStartLocal = ShiftTestData.LocalStart(ShiftTestData.Tomorrow, WorkOrderTestData.NextTime),
                });
        pushes.Should().HaveCount(expected: 2);
    }

    [Fact]
    public async Task MoveWorkOrder_WithStaleRowVersion_RecordsNothing()
    {
        var (tenant, other) = await ArrangeTwoAssigneesAsync();
        var workOrderId = await CreateBacklogAsync(tenant);
        await AssignAsync(tenant, workOrderId, tenant.EmployeeId, WorkOrderTestData.ScheduledTime);
        var staleRowVersion = await _fixture.LoadWorkOrderRowVersionAsync(workOrderId);
        await MoveAsync(tenant, workOrderId, tenant.EmployeeId, WorkOrderTestData.NextTime);
        _fixture.RealtimeQueue.Clear();

        var act = () => MoveAsync(
            tenant, workOrderId, other.EmployeeId, WorkOrderTestData.OverlappingTime, staleRowVersion);

        await act.Should().ThrowAsync<ConflictException>();
        _fixture.RealtimeQueue.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task MoveWorkOrder_WhenTheUnitIsRepeated_RecordsEachEventOnce()
    {
        var (tenant, other) = await ArrangeTwoAssigneesAsync();
        var workOrderId = await CreateBacklogAsync(tenant);
        await AssignAsync(tenant, workOrderId, tenant.EmployeeId, WorkOrderTestData.ScheduledTime);
        _fixture.RealtimeQueue.Clear();
        _fixture.RetryOnce.Arm();

        await MoveAsync(tenant, workOrderId, other.EmployeeId, WorkOrderTestData.NextTime);

        var pushes = PushesOf();
        _fixture.RetryOnce.InjectedCount.Should().Be(expected: 1);
        pushes.Should().HaveCount(expected: 3);
        KindFor(pushes, tenant.IdentityUserId).Should().Be(RealtimeNotificationKind.WorkOrderUnassigned);
        KindFor(pushes, other.IdentityUserId).Should().Be(RealtimeNotificationKind.WorkOrderAssigned);
    }

    private List<RealtimePush> PushesOf() => _fixture.RealtimeQueue.Items.Select(item => item.Push).ToList();

    private static RealtimePush? HintOf(IEnumerable<RealtimePush> pushes, ShiftTenant tenant) =>
        pushes.SingleOrDefault(push =>
            push.Group == RealtimeGroups.ForTopic(tenant.CompanyId, RealtimeTopic.WorkOrders));

    private static RealtimeNotificationKind KindFor(IEnumerable<RealtimePush> pushes, Guid identityUserId) =>
        pushes.Single(push => push.UserId == identityUserId).Notification!.Kind;

    private async Task<ShiftTenant> ArrangeTenantAsync()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        await SeedShiftsAsync(tenant.EmployeeId);

        return tenant;
    }

    private async Task<(ShiftTenant Tenant, EmployeeSeed Other)> ArrangeTwoAssigneesAsync()
    {
        var tenant = await ArrangeTenantAsync();
        _fixture.Clock.Release();
        var other = await _fixture.ArrangeOtherEmployeeAsync(tenant.CompanyId, WorkOrderTestData.DeactivatedEmail);
        await SeedShiftsAsync(other.EmployeeId);

        return (tenant, other);
    }

    private async Task SeedShiftsAsync(Guid employeeId)
    {
        await _fixture.SeedVersionAsync(
            employeeId,
            ShiftTestData.Today.AddDays(value: -30),
            ShiftTestData.Morning,
            ShiftTestData.Afternoon);
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "07:00"));
    }

    private async Task<Guid> CreateBacklogAsync(ShiftTenant tenant)
    {
        var created = await _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.NewRequest(tenant.CompanyId, AccessScope.Company));

        return created.Id;
    }

    private async Task AssignAsync(ShiftTenant tenant, Guid workOrderId, Guid assigneeId, string time) =>
        await RunAsync(
            tenant,
            new AssignWorkOrderRequest
            {
                CompanyId = tenant.CompanyId,
                Id = workOrderId,
                AssigneeId = assigneeId,
                PlannedStartLocal = ShiftTestData.LocalStart(ShiftTestData.Tomorrow, time),
                RowVersion = await _fixture.LoadWorkOrderRowVersionAsync(workOrderId),
            });

    private async Task UnassignAsync(ShiftTenant tenant, Guid workOrderId) =>
        await RunAsync(
            tenant,
            new UnassignWorkOrderRequest
            {
                CompanyId = tenant.CompanyId,
                Id = workOrderId,
                RowVersion = await _fixture.LoadWorkOrderRowVersionAsync(workOrderId),
            });

    private async Task MoveAsync(
        ShiftTenant tenant,
        Guid workOrderId,
        Guid assigneeId,
        string time,
        uint? rowVersion = null
    ) =>
        await RunAsync(
            tenant,
            new MoveWorkOrderRequest
            {
                CompanyId = tenant.CompanyId,
                Id = workOrderId,
                AssigneeId = assigneeId,
                PlannedStartLocal = ShiftTestData.LocalStart(ShiftTestData.Tomorrow, time),
                RowVersion = rowVersion ?? await _fixture.LoadWorkOrderRowVersionAsync(workOrderId),
            });

    private async Task RunAsync<TRequest>(ShiftTenant tenant, TRequest request)
        where TRequest : IRequest
    {
        using var scope = _fixture.CreateUserScope(tenant.CompanyId);

        await scope.ServiceProvider.GetRequiredService<IRequestHandler<TRequest>>().HandleAsync(request);
    }
}
