using System.Collections.Frozen;
using System.Linq.Expressions;
using HealthyFac.Core.Domain.WorkOrders;

namespace HealthyFac.Core.Application.WorkOrders.SearchWorkOrders;

/// <summary>
/// Maps each sort column of the work order grid to its key expression. The priority sorts in list order (low,
/// medium, high, critical), not by the stored text.
/// </summary>
public static class WorkOrderSortMap
{
    public static readonly FrozenDictionary<WorkOrderSortColumn, Expression<Func<WorkOrder, dynamic>>> Instance =
        new Dictionary<WorkOrderSortColumn, Expression<Func<WorkOrder, dynamic>>>
        {
            [WorkOrderSortColumn.Title] = workOrder => workOrder.Title,
            [WorkOrderSortColumn.Priority] = BuildPriorityKey(),
            [WorkOrderSortColumn.PlannedStart] = WorkOrderCurrentRow.Of<dynamic>((_, row) => row.PlannedStartUtc!),
            [WorkOrderSortColumn.CreatedAt] = workOrder => workOrder.CreatedAt,
        }.ToFrozenDictionary();

    /// <summary>
    /// Builds the priority key from <see cref="WorkOrderPriority.Rank"/> as a chain of conditions, one for each
    /// priority, so the rank has one definition and EF Core translates the key to a SQL <c>CASE</c>.
    /// </summary>
    private static Expression<Func<WorkOrder, dynamic>> BuildPriorityKey()
    {
        var workOrder = Expression.Parameter(typeof(WorkOrder), name: "workOrder");
        var priority = Expression.Property(workOrder, nameof(WorkOrder.Priority));
        Expression body = Expression.Constant(WorkOrderPriority.All[^1].Rank);
        foreach (var member in WorkOrderPriority.All.Reverse().Skip(count: 1))
        {
            body = Expression.Condition(
                Expression.Equal(priority, Expression.Constant(member)),
                Expression.Constant(member.Rank),
                body);
        }

        return Expression.Lambda<Func<WorkOrder, dynamic>>(Expression.Convert(body, typeof(object)), workOrder);
    }
}
