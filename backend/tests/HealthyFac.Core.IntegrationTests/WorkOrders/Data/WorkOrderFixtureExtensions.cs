using System.Reflection;
using HealthyFac.Core.Application;
using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.WorkOrders;
using HealthyFac.Core.Application.WorkOrders.SearchWorkOrders;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.WorkOrders.Data;

/// <summary>
/// Arranges and reads work order rows. Every method opens its own DI scope, so it reads what is stored, not tracked
/// objects.
/// </summary>
public static class WorkOrderFixtureExtensions
{
    extension(CoreDatabaseFixture fixture)
    {
        public WorkOrder NewWorkOrder(Guid companyId) =>
            WorkOrder.Create(
                companyId,
                WorkOrderTestData.Title,
                description: null,
                WorkOrderPriority.Medium,
                WorkOrderCategory.Repair,
                machineId: null,
                TimeSpan.FromHours(value: 1));

        public async Task SaveWorkOrderAsync(WorkOrder workOrder, int? currentRevision = null)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

            dbContext.WorkOrders.Add(workOrder);
            if (currentRevision is { } revision)
            {
                dbContext.Entry(workOrder).Property(entity => entity.CurrentRevision).CurrentValue = revision;
            }

            await dbContext.SaveChangesAsync();
        }

        public async Task InsertHistoryRowAsync(Guid workOrderId, int revision)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
            var row = (WorkOrderHistory)Activator.CreateInstance(
                typeof(WorkOrderHistory),
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                [workOrderId, revision, WorkOrderStatus.Backlog, null, null, null],
                culture: null)!;

            dbContext.WorkOrderHistories.Add(row);
            await dbContext.SaveChangesAsync();
        }

        public async Task<CreateWorkOrderResponse> CreateWorkOrderAsync(
            Guid companyId,
            Guid actorUserId,
            CreateWorkOrderRequest request
        )
        {
            using var scope = fixture.CreateActorScope(
                new ActorContext { Kind = ActorKind.User, UserId = actorUserId, CompanyId = companyId });

            return await scope.ServiceProvider
                .GetRequiredService<IRequestHandler<CreateWorkOrderRequest, CreateWorkOrderResponse>>()
                .HandleAsync(request);
        }

        public async Task UpdateWorkOrderAsync(Guid companyId, Guid actorUserId, UpdateWorkOrderRequest request)
        {
            using var scope = fixture.CreateActorScope(
                new ActorContext { Kind = ActorKind.User, UserId = actorUserId, CompanyId = companyId });

            await scope.ServiceProvider
                .GetRequiredService<IRequestHandler<UpdateWorkOrderRequest>>()
                .HandleAsync(request);
        }

        public async Task ChangeWorkOrderStatusAsync(
            Guid companyId,
            Guid actorUserId,
            ChangeWorkOrderStatusRequest request
        )
        {
            using var scope = fixture.CreateActorScope(
                new ActorContext { Kind = ActorKind.User, UserId = actorUserId, CompanyId = companyId });

            await scope.ServiceProvider
                .GetRequiredService<IRequestHandler<ChangeWorkOrderStatusRequest>>()
                .HandleAsync(request);
        }

        public async Task ChangeStatusAsync(
            Guid companyId,
            Guid actorUserId,
            Guid workOrderId,
            WorkOrderStatus status,
            AccessScope? scope = null
        )
        {
            var rowVersion = await fixture.LoadWorkOrderRowVersionAsync(workOrderId);

            await fixture.ChangeWorkOrderStatusAsync(
                companyId,
                actorUserId,
                new ChangeWorkOrderStatusRequest
                {
                    CompanyId = companyId,
                    Scope = scope ?? AccessScope.Company,
                    Id = workOrderId,
                    Status = status,
                    RowVersion = rowVersion,
                });
        }

        public async Task<GridResponse<WorkOrderRowResponse>> SearchWorkOrdersAsync(
            Guid companyId,
            Guid actorUserId,
            SearchWorkOrdersRequest request
        )
        {
            using var scope = fixture.CreateActorScope(
                new ActorContext { Kind = ActorKind.User, UserId = actorUserId, CompanyId = companyId });

            return await scope.ServiceProvider
                .GetRequiredService<IRequestHandler<SearchWorkOrdersRequest, GridResponse<WorkOrderRowResponse>>>()
                .HandleAsync(request);
        }

        public async Task<SearchWorkOrderBatchResponse> SearchWorkOrderBatchAsync(
            Guid companyId,
            Guid actorUserId,
            SearchWorkOrderBatchRequest request
        )
        {
            using var scope = fixture.CreateActorScope(
                new ActorContext { Kind = ActorKind.User, UserId = actorUserId, CompanyId = companyId });

            return await scope.ServiceProvider
                .GetRequiredService<IRequestHandler<SearchWorkOrderBatchRequest, SearchWorkOrderBatchResponse>>()
                .HandleAsync(request);
        }

        public async Task<uint> LoadWorkOrderRowVersionAsync(Guid workOrderId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

            return await dbContext.WorkOrders
                .Where(workOrder => workOrder.Id == workOrderId)
                .Select(workOrder => workOrder.RowVersion)
                .SingleAsync();
        }

        public async Task<int> CountFileLinkRowsAsync(Guid workOrderId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

            return await dbContext.WorkOrderFiles
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .CountAsync(link => link.WorkOrderId == workOrderId);
        }

        public async Task<WorkOrder> LoadWorkOrderAsync(Guid workOrderId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

            return await dbContext.WorkOrders
                .AsNoTracking()
                .Include(workOrder => workOrder.History)
                .Include(workOrder => workOrder.Files)
                .SingleAsync(workOrder => workOrder.Id == workOrderId);
        }

        public async Task<int> CountHistoryRowsAsync(Guid workOrderId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

            return await dbContext.WorkOrderHistories
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .CountAsync(row => row.WorkOrderId == workOrderId);
        }

        public async Task<int> CountWorkOrdersAsync(Guid workOrderId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

            return await dbContext.WorkOrders.CountAsync(workOrder => workOrder.Id == workOrderId);
        }
    }
}
