using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.Shifts;
using HealthyFac.Core.Application.WorkOrders;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Core.IntegrationTests.Shifts.Data;
using HealthyFac.Core.IntegrationTests.WorkOrders.Data;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.WorkOrders;

[Collection(CoreWorkOrdersCollection.Name)]
public sealed class AssignWorkOrderTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task AssignWorkOrder_WithValidSlot_AddsStagedRow()
    {
        var tenant = await ArrangeTenantAsync();
        var workOrderId = await CreateBacklogAsync(tenant);

        await AssignAsync(tenant, workOrderId, WorkOrderTestData.ScheduledTime);

        var workOrder = await _fixture.LoadWorkOrderAsync(workOrderId);

        workOrder.CurrentRevision.Should().Be(expected: 2);
        workOrder.Current.Status.Should().Be(WorkOrderStatus.Staged);
        workOrder.Current.AssigneeId.Should().Be(tenant.EmployeeId);
    }

    [Fact]
    public async Task AssignWorkOrder_OfScheduledWorkOrder_ThrowsNotInBacklog()
    {
        var tenant = await ArrangeTenantAsync();
        var workOrderId = await CreateBacklogAsync(tenant);
        await AssignAsync(tenant, workOrderId, WorkOrderTestData.ScheduledTime);

        var act = () => AssignAsync(tenant, workOrderId, WorkOrderTestData.NextTime);

        await act.Should().ThrowAsync<UnprocessableException>().WithMessage(WorkOrderErrors.Rejections.NotInBacklog);
    }

    [Fact]
    public async Task AssignWorkOrder_ToDeactivatedEmployee_ThrowsNotActive()
    {
        var tenant = await ArrangeTenantAsync();
        var workOrderId = await CreateBacklogAsync(tenant);
        _fixture.Clock.Release();
        var gone = await _fixture.ArrangeOtherEmployeeAsync(
            tenant.CompanyId,
            WorkOrderTestData.DeactivatedEmail,
            isDeactivated: true);
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "07:00"));

        var act = () => AssignAsync(tenant, workOrderId, WorkOrderTestData.ScheduledTime, gone.EmployeeId);

        await act.Should()
            .ThrowAsync<UnprocessableException>()
            .WithMessage(WorkOrderErrors.Rejections.AssigneeNotActive);
    }

    [Fact]
    public async Task AssignWorkOrder_ToEmployeeOfOtherCompany_ThrowsNotFound()
    {
        var tenant = await ArrangeTenantAsync();
        var workOrderId = await CreateBacklogAsync(tenant);
        _fixture.Clock.Release();
        var otherCompanyId = await _fixture.CreateCompanyAsync(WorkOrderTestData.OtherCompanyName);
        var stranger = await _fixture.ArrangeOtherEmployeeAsync(otherCompanyId, WorkOrderTestData.DeactivatedEmail);
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "07:00"));

        var act = () => AssignAsync(tenant, workOrderId, WorkOrderTestData.ScheduledTime, stranger.EmployeeId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Theory]
    [MemberData(nameof(WorkOrderTestData.StartsOutsideTheRules), MemberType = typeof(WorkOrderTestData))]
    public async Task AssignWorkOrder_WithStartOutsideTheRules_ThrowsValidation(
        string caseName,
        int dayOffset,
        string time
    )
    {
        var tenant = await ArrangeTenantAsync();
        var workOrderId = await CreateBacklogAsync(tenant);

        var act = () => AssignOnAsync(tenant, workOrderId, ShiftTestData.Today.AddDays(dayOffset), time);

        await act.Should().ThrowAsync<AppValidationException>(because: caseName);
    }

    [Fact]
    public async Task AssignWorkOrder_AcrossLunch_ThrowsOutsideShift()
    {
        var tenant = await ArrangeTenantAsync();
        var workOrderId = await CreateBacklogAsync(tenant);

        var act = () => AssignAsync(tenant, workOrderId, WorkOrderTestData.LunchTime);

        await act.Should().ThrowAsync<UnprocessableException>().WithMessage(WorkOrderErrors.Rejections.OutsideShift);
    }

    [Fact]
    public async Task AssignWorkOrder_IntoOverlap_ThrowsSlotTaken()
    {
        var tenant = await ArrangeTenantAsync();
        var first = await CreateBacklogAsync(tenant);
        var second = await CreateBacklogAsync(tenant);
        await AssignAsync(tenant, first, WorkOrderTestData.ScheduledTime);

        var act = () => AssignAsync(tenant, second, WorkOrderTestData.OverlappingTime);

        await act.Should().ThrowAsync<UnprocessableException>().WithMessage(WorkOrderErrors.Rejections.SlotTaken);
    }

    [Fact]
    public async Task AssignWorkOrder_StartingWhereAnotherEnds_Succeeds()
    {
        var tenant = await ArrangeTenantAsync();
        var first = await CreateBacklogAsync(tenant);
        var second = await CreateBacklogAsync(tenant);
        await AssignAsync(tenant, first, WorkOrderTestData.ScheduledTime);

        await AssignAsync(tenant, second, WorkOrderTestData.NextTime);

        var workOrder = await _fixture.LoadWorkOrderAsync(second);

        workOrder.Current.Status.Should().Be(WorkOrderStatus.Staged);
    }

    [Fact]
    public async Task MoveWorkOrder_ToFreeSlot_KeepsTheStatus()
    {
        var tenant = await ArrangeTenantAsync();
        var workOrderId = await CreateBacklogAsync(tenant);
        await AssignAsync(tenant, workOrderId, WorkOrderTestData.ScheduledTime);
        await _fixture.ChangeStatusAsync(tenant.CompanyId, tenant.IdentityUserId, workOrderId, WorkOrderStatus.Blocked);

        await RunAsync(
            tenant,
            await Task.FromResult(
                new MoveWorkOrderRequest
                {
                    CompanyId = tenant.CompanyId,
                    Id = workOrderId,
                    PlannedStartLocal = ShiftTestData.LocalStart(ShiftTestData.Tomorrow, WorkOrderTestData.NextTime),
                    RowVersion = await _fixture.LoadWorkOrderRowVersionAsync(workOrderId),
                }));

        var workOrder = await _fixture.LoadWorkOrderAsync(workOrderId);

        workOrder.Current.Status.Should().Be(WorkOrderStatus.Blocked);
        workOrder.Current.PlannedStartLocal.Should()
            .Be(ShiftTestData.LocalStart(ShiftTestData.Tomorrow, WorkOrderTestData.NextTime));
        workOrder.CurrentRevision.Should().Be(expected: 4);
    }

    [Fact]
    public async Task UnassignWorkOrder_OfScheduledWorkOrder_ReturnsItToTheBacklog()
    {
        var tenant = await ArrangeTenantAsync();
        var workOrderId = await CreateBacklogAsync(tenant);
        await AssignAsync(tenant, workOrderId, WorkOrderTestData.ScheduledTime);

        await RunAsync(
            tenant,
            new UnassignWorkOrderRequest
            {
                CompanyId = tenant.CompanyId,
                Id = workOrderId,
                RowVersion = await _fixture.LoadWorkOrderRowVersionAsync(workOrderId),
            });

        var workOrder = await _fixture.LoadWorkOrderAsync(workOrderId);

        workOrder.Current.Status.Should().Be(WorkOrderStatus.Backlog);
        workOrder.Current.AssigneeId.Should().BeNull();
        workOrder.Current.PlannedStartUtc.Should().BeNull();
    }

    [Fact]
    public async Task AssignWorkOrder_WithStaleRowVersion_ThrowsConflict()
    {
        var tenant = await ArrangeTenantAsync();
        var workOrderId = await CreateBacklogAsync(tenant);
        var staleRowVersion = await _fixture.LoadWorkOrderRowVersionAsync(workOrderId);
        await AssignAsync(tenant, workOrderId, WorkOrderTestData.ScheduledTime);
        await RunAsync(
            tenant,
            new UnassignWorkOrderRequest
            {
                CompanyId = tenant.CompanyId,
                Id = workOrderId,
                RowVersion = await _fixture.LoadWorkOrderRowVersionAsync(workOrderId),
            });

        var act = () => RunAsync(
            tenant,
            new AssignWorkOrderRequest
            {
                CompanyId = tenant.CompanyId,
                Id = workOrderId,
                AssigneeId = tenant.EmployeeId,
                PlannedStartLocal = ShiftTestData.LocalStart(ShiftTestData.Tomorrow, WorkOrderTestData.NextTime),
                RowVersion = staleRowVersion,
            });

        await act.Should().ThrowAsync<ConflictException>();
    }

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

    private async Task<Guid> CreateBacklogAsync(ShiftTenant tenant)
    {
        var created = await _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.NewRequest(tenant.CompanyId, AccessScope.Company));

        return created.Id;
    }

    private Task AssignAsync(ShiftTenant tenant, Guid workOrderId, string time, Guid? assigneeId = null) =>
        AssignOnAsync(tenant, workOrderId, ShiftTestData.Tomorrow, time, assigneeId);

    private async Task AssignOnAsync(
        ShiftTenant tenant,
        Guid workOrderId,
        DateOnly date,
        string time,
        Guid? assigneeId = null
    ) =>
        await RunAsync(
            tenant,
            new AssignWorkOrderRequest
            {
                CompanyId = tenant.CompanyId,
                Id = workOrderId,
                AssigneeId = assigneeId ?? tenant.EmployeeId,
                PlannedStartLocal = ShiftTestData.LocalStart(date, time),
                RowVersion = await _fixture.LoadWorkOrderRowVersionAsync(workOrderId),
            });

    private async Task RunAsync<TRequest>(ShiftTenant tenant, TRequest request)
        where TRequest : IRequest
    {
        using var scope = _fixture.CreateUserScope(tenant.CompanyId);

        await scope.ServiceProvider.GetRequiredService<IRequestHandler<TRequest>>().HandleAsync(request);
    }
}
