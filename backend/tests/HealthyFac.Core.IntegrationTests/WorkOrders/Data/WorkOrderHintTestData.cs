using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.WorkOrders;
using HealthyFac.Core.Application.WorkOrders.ApplySchedule;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Shifts.Data;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.WorkOrders.Data;

/// <summary>
/// Arranges the data and returns the change under test, so a test records only that change. The change returns the
/// ID of the work order that it changed.
/// </summary>
public delegate Task<Func<Task<Guid>>> WorkOrderChangeAction(CoreDatabaseFixture fixture, ShiftTenant tenant);

public static class WorkOrderHintTestData
{
    public static TheoryData<string, WorkOrderChangeAction> Changes =>
        new()
        {
            {
                "Create",
                (fixture, tenant) => Task.FromResult<Func<Task<Guid>>>(() => CreateBacklogAsync(fixture, tenant))
            },
            {
                "Update",
                async (fixture, tenant) =>
                {
                    var id = await CreateBacklogAsync(fixture, tenant);
                    var rowVersion = await fixture.LoadWorkOrderRowVersionAsync(id);

                    return () => Returning(
                        id,
                        fixture.UpdateWorkOrderAsync(
                            tenant.CompanyId,
                            Guid.CreateVersion7(),
                            WorkOrderTestData.UpdateRequest(
                                tenant.CompanyId, AccessScope.Company, id, rowVersion, [])));
                }
            },
            {
                "ChangeStatus",
                async (fixture, tenant) =>
                {
                    var id = await CreateAssignedAsync(fixture, tenant, time: "10:00");

                    return () => Returning(
                        id,
                        fixture.ChangeStatusAsync(
                            tenant.CompanyId,
                            Guid.CreateVersion7(),
                            id,
                            WorkOrderStatus.InProgress));
                }
            },
            {
                "Assign",
                async (fixture, tenant) =>
                {
                    var id = await CreateBacklogAsync(fixture, tenant);
                    var rowVersion = await fixture.LoadWorkOrderRowVersionAsync(id);

                    return () => Returning(
                        id,
                        RunAsync(
                            fixture,
                            tenant,
                            new AssignWorkOrderRequest
                            {
                                CompanyId = tenant.CompanyId,
                                Id = id,
                                AssigneeId = tenant.EmployeeId,
                                PlannedStartLocal = ShiftTestData.LocalStart(ShiftTestData.Tomorrow, time: "09:00"),
                                RowVersion = rowVersion,
                            }));
                }
            },
            {
                "Move",
                async (fixture, tenant) =>
                {
                    var id = await CreateAssignedAsync(fixture, tenant, time: "09:00");
                    var rowVersion = await fixture.LoadWorkOrderRowVersionAsync(id);

                    return () => Returning(
                        id,
                        RunAsync(
                            fixture,
                            tenant,
                            new MoveWorkOrderRequest
                            {
                                CompanyId = tenant.CompanyId,
                                Id = id,
                                PlannedStartLocal = ShiftTestData.LocalStart(ShiftTestData.Tomorrow, time: "10:00"),
                                RowVersion = rowVersion,
                            }));
                }
            },
            {
                "Unassign",
                async (fixture, tenant) =>
                {
                    var id = await CreateAssignedAsync(fixture, tenant, time: "09:00");
                    var rowVersion = await fixture.LoadWorkOrderRowVersionAsync(id);

                    return () => Returning(
                        id,
                        RunAsync(
                            fixture,
                            tenant,
                            new UnassignWorkOrderRequest
                                { CompanyId = tenant.CompanyId, Id = id, RowVersion = rowVersion }));
                }
            },
            {
                "Delete",
                async (fixture, tenant) =>
                {
                    var id = await CreateBacklogAsync(fixture, tenant);
                    var rowVersion = await fixture.LoadWorkOrderRowVersionAsync(id);

                    return () => Returning(
                        id,
                        RunAsync(
                            fixture,
                            tenant,
                            new DeleteWorkOrderRequest
                                { CompanyId = tenant.CompanyId, Id = id, RowVersion = rowVersion }));
                }
            },
        };

    public static async Task<ShiftTenant> ArrangeTenantAsync(CoreDatabaseFixture fixture)
    {
        var tenant = await fixture.ArrangeShiftTenantAsync();
        await fixture.SeedVersionAsync(
            tenant.EmployeeId,
            ShiftTestData.Today.AddDays(value: -30),
            ShiftTestData.Morning,
            ShiftTestData.Afternoon);
        fixture.Clock.Freeze(ShiftTestData.NowAt(time: "07:00"));

        return tenant;
    }

    public static async Task<Guid> CreateBacklogAsync(CoreDatabaseFixture fixture, ShiftTenant tenant)
    {
        var created = await fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            Guid.CreateVersion7(),
            WorkOrderTestData.NewRequest(tenant.CompanyId, AccessScope.Company));

        return created.Id;
    }

    public static async Task<ScheduleChange> ApplyChangeAsync(
        CoreDatabaseFixture fixture,
        ShiftTenant tenant,
        Guid id,
        string time
    ) =>
        new()
        {
            WorkOrderId = id,
            RowVersion = await fixture.LoadWorkOrderRowVersionAsync(id),
            AssigneeId = tenant.EmployeeId,
            PlannedStartLocal = ShiftTestData.LocalStart(ShiftTestData.Tomorrow, time),
            EstimateMinutes = WorkOrderTestData.OneHour,
        };

    public static Task<ApplyScheduleChangesResponse> ApplyAsync(
        CoreDatabaseFixture fixture,
        ShiftTenant tenant,
        params ScheduleChange[] changes
    ) =>
        fixture.ReadAsAsync<ApplyScheduleChangesRequest, ApplyScheduleChangesResponse>(
            tenant.CompanyId,
            tenant.IdentityUserId,
            new ApplyScheduleChangesRequest { CompanyId = tenant.CompanyId, Changes = changes });

    public static async Task<Guid> CreateAssignedAsync(CoreDatabaseFixture fixture, ShiftTenant tenant, string time)
    {
        var created = await fixture.CreateWorkOrderAsync(
            tenant.CompanyId,
            Guid.CreateVersion7(),
            WorkOrderTestData.NewRequest(
                tenant.CompanyId,
                AccessScope.Company,
                assigneeId: tenant.EmployeeId,
                plannedStartLocal: ShiftTestData.LocalStart(ShiftTestData.Tomorrow, time)));

        return created.Id;
    }

    private static async Task<Guid> Returning(Guid id, Task change)
    {
        await change;

        return id;
    }

    private static async Task RunAsync<TRequest>(CoreDatabaseFixture fixture, ShiftTenant tenant, TRequest request)
        where TRequest : IRequest
    {
        using var scope = fixture.CreateUserScope(tenant.CompanyId);

        await scope.ServiceProvider.GetRequiredService<IRequestHandler<TRequest>>().HandleAsync(request);
    }
}
