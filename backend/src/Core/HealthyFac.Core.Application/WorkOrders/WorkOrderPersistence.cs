using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Shared.Application.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.WorkOrders;

/// <summary>
/// Loads a work order for a schedule change and saves its new history rows.
/// </summary>
public static class WorkOrderPersistence
{
    extension(ICoreDbContext dbContext)
    {
        /// <summary>
        /// Loads a work order of the company with its current history row, tracked, and sets the original row version
        /// from the client.
        /// </summary>
        /// <param name="companyId">The company of the actor.</param>
        /// <param name="workOrderId">The work order.</param>
        /// <param name="rowVersion">The row version that the client read.</param>
        /// <param name="cancellationToken">Cancels the queries.</param>
        /// <returns>The work order, with <see cref="WorkOrder.Current"/> available.</returns>
        /// <exception cref="NotFoundException">The work order does not exist in the company.</exception>
        public async Task<WorkOrder> LoadForScheduleChangeAsync(
            Guid companyId,
            Guid workOrderId,
            uint rowVersion,
            CancellationToken cancellationToken
        )
        {
            var workOrder = await dbContext.WorkOrders
                    .SingleOrDefaultAsync(
                        candidate => candidate.Id == workOrderId && candidate.CompanyId == companyId,
                        cancellationToken)
                ?? throw new NotFoundException(WorkOrderErrors.Rejections.WorkOrderNotFound);
            await dbContext.WorkOrderHistories
                .Where(row => row.WorkOrderId == workOrder.Id && row.Revision == workOrder.CurrentRevision)
                .LoadAsync(cancellationToken);
            dbContext.Entry(workOrder).Property(stored => stored.RowVersion).OriginalValue = rowVersion;

            return workOrder;
        }

        /// <summary>
        /// Adds the history rows that the work order created since <paramref name="previousRevision"/>.
        /// </summary>
        /// <param name="workOrder">The changed work order.</param>
        /// <param name="previousRevision">The revision before the change.</param>
        public void AddNewHistoryRows(WorkOrder workOrder, int previousRevision) =>
            dbContext.WorkOrderHistories.AddRange(workOrder.History.Where(row => row.Revision > previousRevision));
    }
}
