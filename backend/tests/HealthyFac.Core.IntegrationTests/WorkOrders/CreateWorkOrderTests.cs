using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.WorkOrders;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Core.IntegrationTests.Shifts.Data;
using HealthyFac.Core.IntegrationTests.WorkOrders.Data;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.IntegrationTests.WorkOrders;

[Collection(CoreWorkOrdersCollection.Name)]
public sealed class CreateWorkOrderTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task CreateWorkOrder_WithMachineOfOtherCompany_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        var otherCompanyId = await _fixture.CreateCompanyAsync(WorkOrderTestData.OtherCompanyName);
        var machineId = await _fixture.CreateMachineAsync(
            otherCompanyId,
            WorkOrderTestData.MachineName,
            WorkOrderTestData.MachineSerialNumber);

        var act = () => _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.NewRequest(tenant.CompanyId, AccessScope.Company, machineId));

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(WorkOrderErrors.Rejections.MachineNotFound);
    }

    [Fact]
    public async Task CreateWorkOrder_WithDeletedMachine_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId,
            WorkOrderTestData.MachineName,
            WorkOrderTestData.MachineSerialNumber);
        await _fixture.DeleteMachineAsync(tenant.CompanyId, machineId);

        var act = () => _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.NewRequest(tenant.CompanyId, AccessScope.Company, machineId));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Theory]
    [MemberData(
        nameof(WorkOrderTestData.OwnScopeWithStartCases),
        MemberType = typeof(WorkOrderTestData)
    )]
    public async Task CreateWorkOrder_InOwnScopeWithPlannedStart_ThrowsForbidden(
        string caseName,
        Func<ShiftTenant, Guid?> assigneeOf
    )
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "07:00"));

        var act = () => _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.NewRequest(
                tenant.CompanyId,
                AccessScope.Own,
                assigneeId: assigneeOf(tenant),
                plannedStartLocal: ShiftTestData.LocalStart(ShiftTestData.Tomorrow, WorkOrderTestData.ScheduledTime)));

        await act.Should().ThrowAsync<ForbiddenAccessException>(because: caseName);
    }

    [Fact]
    public async Task CreateWorkOrder_InOwnScopeWithoutEmployee_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();

        var act = () => _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            Guid.CreateVersion7(),
            WorkOrderTestData.NewRequest(tenant.CompanyId, AccessScope.Own));

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(WorkOrderErrors.Rejections.OwnEmployeeNotFound);
    }

    [Fact]
    public async Task CreateWorkOrder_InOwnScope_StartsInBacklogWithCreatorAndFiles()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();

        var response = await _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.NewRequest(tenant.CompanyId, AccessScope.Own));

        var workOrder = await _fixture.LoadWorkOrderAsync(response.Id);

        workOrder.CreatedBy.Should().Be(tenant.IdentityUserId);
        workOrder.Current.Status.Should().Be(WorkOrderStatus.Backlog);
        workOrder.CurrentRevision.Should().Be(expected: 1);
        workOrder.Files.Should().HaveCount(WorkOrderTestData.FileCount);
    }

    [Fact]
    public async Task CreateWorkOrder_InCompanyScopeWithSlot_CreatesAssignedWorkOrder()
    {
        var tenant = await ArrangeScheduledTenantAsync();

        var response = await CreateScheduledAsync(tenant, WorkOrderTestData.ScheduledTime);

        var workOrder = await _fixture.LoadWorkOrderAsync(response.Id);

        workOrder.CurrentRevision.Should().Be(expected: 2);
        workOrder.Current.Status.Should().Be(WorkOrderStatus.Staged);
        workOrder.Current.AssigneeId.Should().Be(tenant.EmployeeId);
        workOrder.Current.PlannedStartLocal.Should()
            .Be(ShiftTestData.LocalStart(ShiftTestData.Tomorrow, WorkOrderTestData.ScheduledTime));
    }

    [Fact]
    public async Task CreateWorkOrder_WithUnknownAssignee_ThrowsNotFound()
    {
        var tenant = await ArrangeScheduledTenantAsync();

        var act = () => CreateScheduledAsync(tenant, WorkOrderTestData.ScheduledTime, Guid.CreateVersion7());

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(WorkOrderErrors.Rejections.AssigneeNotFound);
    }

    [Fact]
    public async Task CreateWorkOrder_WithDeactivatedAssignee_ThrowsUnprocessable()
    {
        var tenant = await ArrangeScheduledTenantAsync();
        _fixture.Clock.Release();
        var gone = await _fixture.ArrangeOtherEmployeeAsync(
            tenant.CompanyId,
            WorkOrderTestData.DeactivatedEmail,
            isDeactivated: true);
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "07:00"));

        var act = () => CreateScheduledAsync(tenant, WorkOrderTestData.ScheduledTime, gone.EmployeeId);

        await act.Should()
            .ThrowAsync<UnprocessableException>()
            .WithMessage(WorkOrderErrors.Rejections.AssigneeNotActive);
    }

    [Theory]
    [MemberData(nameof(WorkOrderTestData.StartsOutsideTheRules), MemberType = typeof(WorkOrderTestData))]
    public async Task CreateWorkOrder_WithStartOutsideTheRules_ThrowsValidation(
        string caseName,
        int dayOffset,
        string time
    )
    {
        var tenant = await ArrangeScheduledTenantAsync();

        var act = () => _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.NewRequest(
                tenant.CompanyId,
                AccessScope.Company,
                assigneeId: tenant.EmployeeId,
                plannedStartLocal: ShiftTestData.LocalStart(ShiftTestData.Today.AddDays(dayOffset), time)));

        await act.Should().ThrowAsync<AppValidationException>(because: caseName);
    }

    [Fact]
    public async Task CreateWorkOrder_WithStartInSkippedDstHour_ThrowsValidation()
    {
        var tenant = await ArrangeScheduledTenantAsync();
        var skippedDate = new DateOnly(year: 2027, month: 3, day: 28);
        _fixture.Clock.Freeze(ShiftTestData.NowOn(skippedDate.AddDays(value: -3), time: "07:00"));
        await _fixture.SeedOverrideAsync(tenant.EmployeeId, skippedDate, "02:00-05:00");

        var act = () => _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.NewRequest(
                tenant.CompanyId,
                AccessScope.Company,
                assigneeId: tenant.EmployeeId,
                plannedStartLocal: ShiftTestData.LocalStart(skippedDate, time: "03:30")));

        await act.Should().ThrowAsync<AppValidationException>();
    }

    [Fact]
    public async Task CreateWorkOrder_WithSlotAcrossLunch_ThrowsOutsideShift()
    {
        var tenant = await ArrangeScheduledTenantAsync();

        var act = () => CreateScheduledAsync(tenant, WorkOrderTestData.LunchTime);

        await act.Should().ThrowAsync<UnprocessableException>().WithMessage(WorkOrderErrors.Rejections.OutsideShift);
    }

    [Fact]
    public async Task CreateWorkOrder_WithOverlappingSlot_ThrowsSlotTaken()
    {
        var tenant = await ArrangeScheduledTenantAsync();
        await CreateScheduledAsync(tenant, WorkOrderTestData.ScheduledTime);

        var act = () => CreateScheduledAsync(tenant, WorkOrderTestData.OverlappingTime);

        await act.Should().ThrowAsync<UnprocessableException>().WithMessage(WorkOrderErrors.Rejections.SlotTaken);
    }

    [Fact]
    public async Task CreateWorkOrder_WithSlotStartingWhereAnotherEnds_Succeeds()
    {
        var tenant = await ArrangeScheduledTenantAsync();
        await CreateScheduledAsync(tenant, WorkOrderTestData.ScheduledTime);

        var response = await CreateScheduledAsync(tenant, WorkOrderTestData.NextTime);

        var workOrder = await _fixture.LoadWorkOrderAsync(response.Id);

        workOrder.Current.Status.Should().Be(WorkOrderStatus.Staged);
    }

    private async Task<ShiftTenant> ArrangeScheduledTenantAsync()
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

    private Task<CreateWorkOrderResponse> CreateScheduledAsync(
        ShiftTenant tenant,
        string time,
        Guid? assigneeId = null
    ) =>
        _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.NewRequest(
                tenant.CompanyId,
                AccessScope.Company,
                assigneeId: assigneeId ?? tenant.EmployeeId,
                plannedStartLocal: ShiftTestData.LocalStart(ShiftTestData.Tomorrow, time)));
}
