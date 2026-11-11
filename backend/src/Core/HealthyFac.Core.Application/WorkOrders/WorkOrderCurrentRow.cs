using System.Linq.Expressions;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Shared.Application.QueryExpressions;

namespace HealthyFac.Core.Application.WorkOrders;

/// <summary>
/// Defines the current history row of a work order for queries: the row with the revision
/// <see cref="WorkOrder.CurrentRevision"/>. The filters, the sort, and the projection of the grid and the board share
/// it, so the rule has one definition. EF Core translates the result to a correlated subquery.
/// </summary>
public static class WorkOrderCurrentRow
{
    /// <summary>The current history row of a work order.</summary>
    public static readonly Expression<Func<WorkOrder, WorkOrderHistory>> Selector =
        workOrder => workOrder.History.First(row => row.Revision == workOrder.CurrentRevision);

    /// <summary>
    /// Builds a lambda over a work order from a template over the work order and its current row. The current row
    /// parameter of the template is replaced by <see cref="Selector"/>, so EF Core translates the result without
    /// LINQKit.
    /// </summary>
    /// <param name="template">The template with a parameter for the current row.</param>
    /// <typeparam name="T">The result type.</typeparam>
    /// <returns>The lambda over the work order.</returns>
    public static Expression<Func<WorkOrder, T>> Of<T>(Expression<Func<WorkOrder, WorkOrderHistory, T>> template)
    {
        var workOrderParameter = template.Parameters[index: 0];
        var rowBody = new ParameterReplacer(Selector.Parameters[index: 0], workOrderParameter).Visit(Selector.Body);
        var body = new ParameterReplacer(template.Parameters[index: 1], rowBody).Visit(template.Body);

        return Expression.Lambda<Func<WorkOrder, T>>(body, workOrderParameter);
    }

    /// <summary>
    /// The delayed rule: a staged work order whose planned start is before now.
    /// </summary>
    /// <param name="nowUtc">The current UTC instant.</param>
    /// <returns>The predicate over a work order.</returns>
    public static Expression<Func<WorkOrder, bool>> IsDelayed(DateTime nowUtc) =>
        Of<bool>((_, row) => row.Status == WorkOrderStatus.Staged && row.PlannedStartUtc < nowUtc);

    /// <summary>
    /// The opposite of <see cref="IsDelayed"/>. It checks the planned start for <see langword="null"/> on purpose: a
    /// backlog work order has no start, and SQL negation of an unknown comparison would drop it.
    /// </summary>
    /// <param name="nowUtc">The current UTC instant.</param>
    /// <returns>The predicate over a work order.</returns>
    public static Expression<Func<WorkOrder, bool>> IsNotDelayed(DateTime nowUtc) =>
        Of<bool>((_, row) => !(row.Status == WorkOrderStatus.Staged
            && row.PlannedStartUtc != null
            && row.PlannedStartUtc < nowUtc));
}
