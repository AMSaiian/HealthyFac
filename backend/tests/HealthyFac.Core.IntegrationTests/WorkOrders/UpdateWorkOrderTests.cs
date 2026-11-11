using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.WorkOrders;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Shifts.Data;
using HealthyFac.Core.IntegrationTests.WorkOrders.Data;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.IntegrationTests.WorkOrders;

[Collection(CoreWorkOrdersCollection.Name)]
public sealed class UpdateWorkOrderTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task UpdateWorkOrder_WithAddedAndRemovedFiles_ReplacesTheLinks()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        var workOrderId = await CreateBacklogAsync(tenant, WorkOrderTestData.FileA, WorkOrderTestData.FileB);
        var rowVersion = await _fixture.LoadWorkOrderRowVersionAsync(workOrderId);

        await _fixture.UpdateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.UpdateRequest(
                tenant.CompanyId,
                AccessScope.Company,
                workOrderId,
                rowVersion,
                [WorkOrderTestData.FileB, WorkOrderTestData.FileC]));

        var workOrder = await _fixture.LoadWorkOrderAsync(workOrderId);
        var linkRows = await _fixture.CountFileLinkRowsAsync(workOrderId);

        workOrder.Files.Select(link => link.FileId)
            .Should()
            .BeEquivalentTo([WorkOrderTestData.FileB, WorkOrderTestData.FileC]);
        workOrder.Title.Should().Be(WorkOrderTestData.ChangedTitle);
        workOrder.Priority.Should().Be(WorkOrderPriority.Critical);
        linkRows.Should().Be(expected: 3);
    }

    [Fact]
    public async Task UpdateWorkOrder_WithUnchangedFiles_AddsNoLinkRow()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        var workOrderId = await CreateBacklogAsync(tenant, WorkOrderTestData.FileA, WorkOrderTestData.FileB);
        var rowVersion = await _fixture.LoadWorkOrderRowVersionAsync(workOrderId);

        await _fixture.UpdateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.UpdateRequest(
                tenant.CompanyId,
                AccessScope.Company,
                workOrderId,
                rowVersion,
                [WorkOrderTestData.FileB, WorkOrderTestData.FileA]));

        var linkRows = await _fixture.CountFileLinkRowsAsync(workOrderId);
        var newRowVersion = await _fixture.LoadWorkOrderRowVersionAsync(workOrderId);

        linkRows.Should().Be(expected: 2);
        newRowVersion.Should().NotBe(rowVersion);
    }

    [Fact]
    public async Task UpdateWorkOrder_WithStaleRowVersion_ThrowsConflictAndChangesNothing()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        var workOrderId = await CreateBacklogAsync(tenant, WorkOrderTestData.FileA);
        var staleRowVersion = await _fixture.LoadWorkOrderRowVersionAsync(workOrderId);
        await _fixture.UpdateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.UpdateRequest(
                tenant.CompanyId,
                AccessScope.Company,
                workOrderId,
                staleRowVersion,
                [WorkOrderTestData.FileA]));

        var act = () => _fixture.UpdateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.UpdateRequest(
                tenant.CompanyId,
                AccessScope.Company,
                workOrderId,
                staleRowVersion,
                [WorkOrderTestData.FileB],
                title: WorkOrderTestData.Title));

        await act.Should().ThrowAsync<ConflictException>();
        var workOrder = await _fixture.LoadWorkOrderAsync(workOrderId);

        workOrder.Title.Should().Be(WorkOrderTestData.ChangedTitle);
        workOrder.Files.Select(link => link.FileId).Should().Equal(WorkOrderTestData.FileA);
    }

    [Fact]
    public async Task UpdateWorkOrder_InOwnScopeWithNewEstimate_ThrowsForbidden()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        var workOrderId = await CreateBacklogAsync(tenant, WorkOrderTestData.FileA);
        var rowVersion = await _fixture.LoadWorkOrderRowVersionAsync(workOrderId);

        var act = () => _fixture.UpdateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.UpdateRequest(
                tenant.CompanyId,
                AccessScope.Own,
                workOrderId,
                rowVersion,
                [WorkOrderTestData.FileA],
                WorkOrderTestData.LongerEstimate));

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task UpdateWorkOrder_WithLongerEstimateThatOverlapsTheNextOne_ThrowsSlotTaken()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        await _fixture.SeedVersionAsync(
            tenant.EmployeeId,
            ShiftTestData.Today.AddDays(value: -30),
            ShiftTestData.Morning,
            ShiftTestData.Afternoon);
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "07:00"));
        var first = await CreateScheduledAsync(tenant, WorkOrderTestData.ScheduledTime);
        await CreateScheduledAsync(tenant, WorkOrderTestData.NextTime);
        var rowVersion = await _fixture.LoadWorkOrderRowVersionAsync(first.Id);

        var act = () => _fixture.UpdateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.UpdateRequest(
                tenant.CompanyId,
                AccessScope.Company,
                first.Id,
                rowVersion,
                [],
                WorkOrderTestData.LongerEstimate));

        await act.Should().ThrowAsync<UnprocessableException>().WithMessage(WorkOrderErrors.Rejections.SlotTaken);
        var workOrder = await _fixture.LoadWorkOrderAsync(first.Id);

        workOrder.Estimate.Should().Be(TimeSpan.FromMinutes(WorkOrderTestData.OneHour));
    }

    private async Task<Guid> CreateBacklogAsync(ShiftTenant tenant, params Guid[] fileIds)
    {
        var response = await _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.WithFiles(
                WorkOrderTestData.NewRequest(tenant.CompanyId, AccessScope.Company),
                fileIds));

        return response.Id;
    }

    private Task<CreateWorkOrderResponse> CreateScheduledAsync(ShiftTenant tenant, string time) =>
        _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.NewRequest(
                tenant.CompanyId,
                AccessScope.Company,
                assigneeId: tenant.EmployeeId,
                plannedStartLocal: ShiftTestData.LocalStart(ShiftTestData.Tomorrow, time)));
}
