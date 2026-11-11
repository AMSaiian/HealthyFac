using HealthyFac.Core.Application;
using HealthyFac.Core.Application.Shifts;
using HealthyFac.Core.Domain.Shifts;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Domain.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Shifts.Data;

/// <summary>
/// Arranges shift data and runs the shift use cases as a user of the company. Every method opens its own DI scope, so
/// it reads what is stored, not tracked objects.
/// </summary>
public static class ShiftFixtureExtensions
{
    extension(CoreDatabaseFixture fixture)
    {
        public async Task<ShiftTenant> ArrangeShiftTenantAsync()
        {
            var companyId = await fixture.CreateCompanyAsync(ShiftTestData.CompanyName);
            var seed = await fixture.ArrangeEmployeeAsync(
                companyId,
                new EmployeeSeedOptions
                {
                    Email = ShiftTestData.EmployeeEmail,
                    FirstName = ShiftTestData.EmployeeFirstName,
                    LastName = ShiftTestData.EmployeeLastName,
                });

            return new ShiftTenant
            {
                CompanyId = companyId,
                EmployeeId = seed.EmployeeId,
                IdentityUserId = seed.IdentityUserId,
            };
        }

        public async Task<Guid> CreateVersionAsync(
            ShiftTenant tenant,
            DateOnly effectiveFrom,
            IReadOnlyList<ShiftTemplateDayInput> days
        )
        {
            using var scope = fixture.CreateUserScope(tenant.CompanyId);
            var response = await scope.ServiceProvider
                .GetRequiredService<
                    IRequestHandler<CreateShiftTemplateVersionRequest, CreateShiftTemplateVersionResponse>>()
                .HandleAsync(
                    new CreateShiftTemplateVersionRequest
                    {
                        CompanyId = tenant.CompanyId,
                        EmployeeId = tenant.EmployeeId,
                        EffectiveFrom = effectiveFrom,
                        Days = days,
                    });

            return response.Id;
        }

        public async Task UpdateVersionAsync(
            ShiftTenant tenant,
            Guid versionId,
            IReadOnlyList<ShiftTemplateDayInput> days,
            uint rowVersion
        )
        {
            using var scope = fixture.CreateUserScope(tenant.CompanyId);

            await scope.ServiceProvider
                .GetRequiredService<IRequestHandler<UpdateShiftTemplateVersionRequest>>()
                .HandleAsync(
                    new UpdateShiftTemplateVersionRequest
                    {
                        CompanyId = tenant.CompanyId,
                        EmployeeId = tenant.EmployeeId,
                        VersionId = versionId,
                        Days = days,
                        RowVersion = rowVersion,
                    });
        }

        public async Task DeleteVersionAsync(ShiftTenant tenant, Guid versionId, uint rowVersion)
        {
            using var scope = fixture.CreateUserScope(tenant.CompanyId);

            await scope.ServiceProvider
                .GetRequiredService<IRequestHandler<DeleteShiftTemplateVersionRequest>>()
                .HandleAsync(
                    new DeleteShiftTemplateVersionRequest
                    {
                        CompanyId = tenant.CompanyId,
                        EmployeeId = tenant.EmployeeId,
                        VersionId = versionId,
                        RowVersion = rowVersion,
                    });
        }

        public async Task SetOverrideAsync(
            ShiftTenant tenant,
            DateOnly date,
            IReadOnlyList<ShiftBlockInput> blocks,
            uint? rowVersion = null
        )
        {
            using var scope = fixture.CreateUserScope(tenant.CompanyId);

            await scope.ServiceProvider
                .GetRequiredService<IRequestHandler<SetShiftOverrideRequest>>()
                .HandleAsync(
                    new SetShiftOverrideRequest
                    {
                        CompanyId = tenant.CompanyId,
                        EmployeeId = tenant.EmployeeId,
                        Date = date,
                        Blocks = blocks,
                        RowVersion = rowVersion,
                    });
        }

        public async Task DeleteOverrideAsync(ShiftTenant tenant, DateOnly date, uint rowVersion)
        {
            using var scope = fixture.CreateUserScope(tenant.CompanyId);

            await scope.ServiceProvider
                .GetRequiredService<IRequestHandler<DeleteShiftOverrideRequest>>()
                .HandleAsync(
                    new DeleteShiftOverrideRequest
                    {
                        CompanyId = tenant.CompanyId,
                        EmployeeId = tenant.EmployeeId,
                        Date = date,
                        RowVersion = rowVersion,
                    });
        }

        public async Task<uint> LoadOverrideRowVersionAsync(Guid employeeId, DateOnly date)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

            return await dbContext.ShiftOverrides
                .Where(shiftOverride => shiftOverride.EmployeeId == employeeId && shiftOverride.Date == date)
                .Select(shiftOverride => shiftOverride.RowVersion)
                .SingleAsync();
        }

        public async Task<EmployeeSeed> ArrangeOtherEmployeeAsync(
            Guid companyId,
            string email,
            bool isDeactivated = false
        ) =>
            await fixture.ArrangeEmployeeAsync(
                companyId,
                new EmployeeSeedOptions
                {
                    Email = email,
                    FirstName = ShiftTestData.OtherFirstName,
                    LastName = ShiftTestData.EmployeeLastName,
                    IsDeactivated = isDeactivated,
                });

        public async Task<TResponse> ReadAsAsync<TRequest, TResponse>(
            Guid companyId,
            Guid actorUserId,
            TRequest request
        )
            where TRequest : IRequest<TResponse>
        {
            using var scope = fixture.CreateActorScope(
                new ActorContext { Kind = ActorKind.User, UserId = actorUserId, CompanyId = companyId });

            return await scope.ServiceProvider
                .GetRequiredService<IRequestHandler<TRequest, TResponse>>()
                .HandleAsync(request);
        }

        public async Task CompleteWorkOrderAsync(Guid workOrderId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
            var workOrder = await dbContext.WorkOrders
                .Include(candidate => candidate.History)
                .SingleAsync(candidate => candidate.Id == workOrderId);

            workOrder.ChangeStatus(WorkOrderStatus.Completed);
            await dbContext.SaveChangesAsync();
        }

        public async Task<Guid> SeedVersionAsync(Guid employeeId, DateOnly effectiveFrom, params string[] ranges)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
            var version = new ShiftTemplateVersion(employeeId, effectiveFrom, ShiftTestData.WorkWeekBlocks(ranges));

            dbContext.ShiftTemplateVersions.Add(version);
            await dbContext.SaveChangesAsync();

            return version.Id;
        }

        public async Task SeedOverrideAsync(Guid employeeId, DateOnly date, params string[] ranges)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

            dbContext.ShiftOverrides.Add(
                new ShiftOverride(employeeId, date, ShiftTestData.Blocks(ranges).Select(block => block.ToTimes())));
            await dbContext.SaveChangesAsync();
        }

        public async Task<uint> LoadRowVersionAsync(Guid versionId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

            return await dbContext.ShiftTemplateVersions
                .Where(version => version.Id == versionId)
                .Select(version => version.RowVersion)
                .SingleAsync();
        }

        public async Task<DaySchedule> ResolveDayAsync(ShiftTenant tenant, DateOnly date)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var reader = scope.ServiceProvider.GetRequiredService<DayScheduleReader>();
            var zone = await reader.GetZoneAsync(tenant.CompanyId, CancellationToken.None);
            var shifts = await reader.LoadAsync(
                tenant.EmployeeId,
                zone,
                date,
                date,
                isTracked: false,
                CancellationToken.None);

            return shifts.Resolve(date, date).Single();
        }

        public async Task<int> CountLiveBlocksAsync(Guid versionId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

            return await dbContext.ShiftTemplateBlocks.CountAsync(block => block.ShiftTemplateVersionId == versionId);
        }

        public async Task<Guid> ArrangeStagedWorkOrderAsync(
            ShiftTenant tenant,
            DateOnly date,
            string time,
            int minutes
        )
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
            var localStart = ShiftTestData.LocalStart(date, time);
            var workOrder = WorkOrder.Create(
                tenant.CompanyId,
                title: "Shift guard order",
                description: null,
                WorkOrderPriority.Medium,
                WorkOrderCategory.Repair,
                machineId: null,
                TimeSpan.FromMinutes(minutes));
            workOrder.Assign(tenant.EmployeeId, localStart, LocalTimes.ToUtc(localStart, ShiftTestData.Zone));

            dbContext.WorkOrders.Add(workOrder);
            await dbContext.SaveChangesAsync();

            return workOrder.Id;
        }
    }
}

public sealed record ShiftTenant
{
    public required Guid CompanyId { get; init; }

    public required Guid EmployeeId { get; init; }

    public required Guid IdentityUserId { get; init; }
}
