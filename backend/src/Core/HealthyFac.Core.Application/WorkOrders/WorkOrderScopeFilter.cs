using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Domain.WorkOrders;

namespace HealthyFac.Core.Application.WorkOrders;

/// <summary>
/// Narrows a query of work orders to the rows that the access scope reaches.
/// </summary>
public static class WorkOrderScopeFilter
{
    extension(IQueryable<WorkOrder> query)
    {
        /// <summary>
        /// The company scope reaches every work order of the company. The own scope reaches the work orders whose
        /// current assignee is the employee of the actor, and the backlog work orders that the actor created.
        /// </summary>
        /// <param name="scope">The access scope of the actor.</param>
        /// <param name="companyId">The company of the actor.</param>
        /// <param name="ownEmployeeId">
        /// The employee of the actor, or <see langword="null"/> when there is none. An own-scope actor without an
        /// employee reaches no work order, not even the backlog rows that the user created.
        /// </param>
        /// <param name="actorUserId">The user ID of the actor.</param>
        /// <returns>The narrowed query.</returns>
        public IQueryable<WorkOrder> ApplyScope(
            AccessScope scope,
            Guid companyId,
            Guid? ownEmployeeId,
            Guid actorUserId
        )
        {
            if (scope == AccessScope.Company)
            {
                return query.Where(workOrder => workOrder.CompanyId == companyId);
            }

            if (ownEmployeeId is not { } ownId)
            {
                return query.Where(_ => false);
            }

            return query.Where(workOrder => workOrder.CompanyId == companyId
                && workOrder.History.Any(row => row.Revision == workOrder.CurrentRevision
                    && (row.AssigneeId == ownId
                        || (row.Status == WorkOrderStatus.Backlog && workOrder.CreatedBy == actorUserId))));
        }
    }
}
