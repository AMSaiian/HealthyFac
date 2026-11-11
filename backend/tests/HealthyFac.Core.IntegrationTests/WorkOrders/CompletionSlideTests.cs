using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Shifts.Data;
using HealthyFac.Core.IntegrationTests.WorkOrders.Data;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.IntegrationTests.WorkOrders;

[Collection(CoreWorkOrdersCollection.Name)]
public sealed class CompletionSlideTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task Complete_Late_PushesStagedOnesAcrossLunchAndKeepsFixedOnes()
    {
        var tenant = await ArrangeTenantAsync();
        var finishing = await CreateAssignedAsync(tenant, time: "10:00");
        var next = await CreateAssignedAsync(tenant, time: "11:00");
        var afternoon = await CreateAssignedAsync(tenant, time: "13:00");
        var fixedOrder = await CreateAssignedAsync(tenant, time: "15:00");
        await _fixture.ChangeStatusAsync(
            tenant.CompanyId, tenant.IdentityUserId, finishing, WorkOrderStatus.InProgress);
        await _fixture.ChangeStatusAsync(
            tenant.CompanyId, tenant.IdentityUserId, fixedOrder, WorkOrderStatus.InProgress);
        var nextRowVersion = await _fixture.LoadWorkOrderRowVersionAsync(next);
        _fixture.Clock.Freeze(ShiftTestData.NowOn(ShiftTestData.Tomorrow, time: "11:30"));

        await _fixture.ChangeStatusAsync(tenant.CompanyId, tenant.IdentityUserId, finishing, WorkOrderStatus.Completed);

        var movedNext = await _fixture.LoadWorkOrderAsync(next);
        var movedAfternoon = await _fixture.LoadWorkOrderAsync(afternoon);
        var kept = await _fixture.LoadWorkOrderAsync(fixedOrder);
        var newNextRowVersion = await _fixture.LoadWorkOrderRowVersionAsync(next);

        movedNext.Current.Status.Should().Be(WorkOrderStatus.Staged);
        movedNext.Current.PlannedStartLocal.Should()
            .Be(ShiftTestData.LocalStart(ShiftTestData.Tomorrow, time: "13:00"));
        movedNext.CurrentRevision.Should().Be(expected: 3);
        movedAfternoon.Current.PlannedStartLocal.Should()
            .Be(ShiftTestData.LocalStart(ShiftTestData.Tomorrow, time: "14:00"));
        kept.Current.Status.Should().Be(WorkOrderStatus.InProgress);
        kept.Current.PlannedStartLocal.Should().Be(ShiftTestData.LocalStart(ShiftTestData.Tomorrow, time: "15:00"));
        newNextRowVersion.Should().NotBe(nextRowVersion);
    }

    [Fact]
    public async Task Complete_Late_SendsAWorkOrderThatFitsNowhereToTheBacklog()
    {
        var tenant = await ArrangeTenantAsync();
        var finishing = await CreateAssignedAsync(tenant, time: "15:00");
        var last = await CreateAssignedAsync(tenant, time: "16:00");
        await _fixture.ChangeStatusAsync(
            tenant.CompanyId, tenant.IdentityUserId, finishing, WorkOrderStatus.InProgress);
        _fixture.Clock.Freeze(ShiftTestData.NowOn(ShiftTestData.Tomorrow, time: "16:30"));

        await _fixture.ChangeStatusAsync(tenant.CompanyId, tenant.IdentityUserId, finishing, WorkOrderStatus.Completed);

        var moved = await _fixture.LoadWorkOrderAsync(last);

        moved.Current.Status.Should().Be(WorkOrderStatus.Backlog);
        moved.Current.AssigneeId.Should().BeNull();
        moved.Current.PlannedStartUtc.Should().BeNull();
    }

    [Fact]
    public async Task Complete_Early_MovesNothing()
    {
        var tenant = await ArrangeTenantAsync();
        var finishing = await CreateAssignedAsync(tenant, time: "10:00");
        var next = await CreateAssignedAsync(tenant, time: "11:00");
        await _fixture.ChangeStatusAsync(
            tenant.CompanyId, tenant.IdentityUserId, finishing, WorkOrderStatus.InProgress);
        _fixture.Clock.Freeze(ShiftTestData.NowOn(ShiftTestData.Tomorrow, time: "10:30"));

        await _fixture.ChangeStatusAsync(tenant.CompanyId, tenant.IdentityUserId, finishing, WorkOrderStatus.Completed);

        var untouched = await _fixture.LoadWorkOrderAsync(next);

        untouched.CurrentRevision.Should().Be(expected: 2);
        untouched.Current.PlannedStartLocal.Should()
            .Be(ShiftTestData.LocalStart(ShiftTestData.Tomorrow, time: "11:00"));
    }

    [Fact]
    public async Task UpdateWorkOrder_WithRowVersionFromBeforeTheSlide_ThrowsConflict()
    {
        var tenant = await ArrangeTenantAsync();
        var finishing = await CreateAssignedAsync(tenant, time: "10:00");
        var next = await CreateAssignedAsync(tenant, time: "11:00");
        await _fixture.ChangeStatusAsync(
            tenant.CompanyId, tenant.IdentityUserId, finishing, WorkOrderStatus.InProgress);
        var staleRowVersion = await _fixture.LoadWorkOrderRowVersionAsync(next);
        _fixture.Clock.Freeze(ShiftTestData.NowOn(ShiftTestData.Tomorrow, time: "11:30"));
        await _fixture.ChangeStatusAsync(tenant.CompanyId, tenant.IdentityUserId, finishing, WorkOrderStatus.Completed);

        var act = () => _fixture.UpdateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.UpdateRequest(
                tenant.CompanyId,
                AccessScope.Company,
                next,
                staleRowVersion,
                [],
                title: WorkOrderTestData.ChangedTitle));

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

    private async Task<Guid> CreateAssignedAsync(ShiftTenant tenant, string time)
    {
        var created = await _fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            tenant.IdentityUserId,
            WorkOrderTestData.NewRequest(
                tenant.CompanyId,
                AccessScope.Company,
                assigneeId: tenant.EmployeeId,
                plannedStartLocal: ShiftTestData.LocalStart(ShiftTestData.Tomorrow, time)));

        return created.Id;
    }
}
