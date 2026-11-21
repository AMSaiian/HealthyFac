using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Shared.Application.Realtime;

namespace HealthyFac.Core.Application.Realtime;

/// <summary>
/// Adds the real-time events of a work order change and pushes them. Every method reads no data and does no I/O,
/// because the use case read the identity users inside its transaction operation. A push never fails a committed
/// change.
/// </summary>
public static class WorkOrderChangeNotifications
{
    extension(IRealtimeNotifier notifier)
    {
        /// <summary>
        /// Adds the events of an assignment change and the <c>workOrders</c> hint, then pushes. A new assignee gets
        /// <c>workOrderAssigned</c>, an old assignee gets <c>workOrderUnassigned</c>, and the same assignee gets
        /// <c>workOrderRescheduled</c>. Call it after the save returned.
        /// </summary>
        /// <param name="companyId">The company of the work order.</param>
        /// <param name="change">The assignment before and after.</param>
        public void PushAssignmentChange(Guid companyId, WorkOrderAssignmentChange change)
        {
            var isSameAssignee = change.OldAssigneeId is not null && change.OldAssigneeId == change.NewAssigneeId;
            if (isSameAssignee)
            {
                notifier.AddRescheduled(change);
            }
            else
            {
                notifier.AddUnassigned(change);
                notifier.AddAssigned(change);
            }

            notifier.PushWorkOrdersHint(companyId);
        }

        /// <summary>
        /// Adds <c>workOrderEstimateChanged</c> for the assignee, when the estimate changed and the assignee has an
        /// identity user, and the <c>workOrders</c> hint, then pushes. Call it after the save returned.
        /// </summary>
        /// <param name="companyId">The company of the work order.</param>
        /// <param name="change">The estimate before and after.</param>
        public void PushEstimateChange(Guid companyId, WorkOrderEstimateChange change)
        {
            if (change.AssigneeUserId is { } userId && change.OldEstimate != change.NewEstimate)
            {
                notifier.AddToUser(
                    userId,
                    RealtimeNotificationKind.WorkOrderEstimateChanged,
                    new WorkOrderEstimateChangedData
                    {
                        WorkOrderId = change.WorkOrderId,
                        Title = change.Title,
                        OldEstimate = change.OldEstimate,
                        NewEstimate = change.NewEstimate,
                    },
                    isActorSkipped: true);
            }

            notifier.PushWorkOrdersHint(companyId);
        }

        /// <summary>
        /// Adds <c>workOrderBlocked</c> for the company group when the new status is blocked, the moves of a late
        /// completion, and the <c>workOrders</c> hint, then pushes. A moved work order gets
        /// <c>workOrderRescheduled</c>, and a work order sent to the backlog gets <c>workOrderReturnedToBacklog</c>.
        /// Call it after the save returned.
        /// </summary>
        /// <param name="companyId">The company of the work order.</param>
        /// <param name="change">The status change and the moves of the slide.</param>
        public void PushStatusChange(Guid companyId, WorkOrderStatusChange change)
        {
            if (change.Status == WorkOrderStatus.Blocked)
            {
                notifier.AddToGroup(
                    companyId,
                    RealtimeNotificationKind.WorkOrderBlocked,
                    new WorkOrderBlockedData { WorkOrderId = change.WorkOrderId, Title = change.Title },
                    isActorSkipped: true);
            }

            foreach (var move in change.SlideMoves)
            {
                notifier.AddSlideMove(move);
            }

            notifier.PushWorkOrdersHint(companyId);
        }

        /// <summary>
        /// Adds the <c>workOrders</c> hint and pushes. Call it after the save returned.
        /// </summary>
        /// <param name="companyId">The company of the work order.</param>
        public void PushWorkOrdersHint(Guid companyId)
        {
            notifier.AddHint(companyId, new RealtimeHint { Topic = RealtimeTopic.WorkOrders });
            notifier.Push();
        }

        private void AddRescheduled(WorkOrderAssignmentChange change)
        {
            if (change.NewAssigneeUserId is { } userId)
            {
                notifier.AddToUser(
                    userId,
                    RealtimeNotificationKind.WorkOrderRescheduled,
                    new WorkOrderRescheduledData
                    {
                        WorkOrderId = change.WorkOrderId,
                        Title = change.Title,
                        PlannedStartLocal = change.PlannedStartLocal!.Value,
                    },
                    isActorSkipped: true);
            }
        }

        private void AddUnassigned(WorkOrderAssignmentChange change)
        {
            if (change.OldAssigneeUserId is { } userId)
            {
                notifier.AddToUser(
                    userId,
                    RealtimeNotificationKind.WorkOrderUnassigned,
                    new WorkOrderTitleData { Title = change.Title },
                    isActorSkipped: true);
            }
        }

        private void AddAssigned(WorkOrderAssignmentChange change)
        {
            if (change.NewAssigneeUserId is { } userId)
            {
                notifier.AddToUser(
                    userId,
                    RealtimeNotificationKind.WorkOrderAssigned,
                    new WorkOrderAssignedData
                    {
                        WorkOrderId = change.WorkOrderId,
                        Title = change.Title,
                        PlannedStartLocal = change.PlannedStartLocal!.Value,
                    },
                    isActorSkipped: true);
            }
        }

        private void AddSlideMove(WorkOrderSlideMove move)
        {
            if (move.AssigneeUserId is not { } userId)
            {
                return;
            }

            if (move.NewStartLocal is { } newStart)
            {
                notifier.AddToUser(
                    userId,
                    RealtimeNotificationKind.WorkOrderRescheduled,
                    new WorkOrderRescheduledData
                    {
                        WorkOrderId = move.WorkOrderId,
                        Title = move.Title,
                        PlannedStartLocal = newStart,
                    },
                    isActorSkipped: false);

                return;
            }

            notifier.AddToUser(
                userId,
                RealtimeNotificationKind.WorkOrderReturnedToBacklog,
                new WorkOrderTitleData { Title = move.Title },
                isActorSkipped: false);
        }
    }
}
