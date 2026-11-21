using HealthyFac.Shared.Application.Realtime;

namespace HealthyFac.Core.Application.Realtime;

/// <summary>
/// Adds the real-time events of a shift change and pushes them.
/// </summary>
public static class ShiftChangeNotifications
{
    extension(IRealtimeNotifier notifier)
    {
        /// <summary>
        /// Adds <c>shiftChanged</c> for the user of the employee, when the employee has an identity user, and the
        /// <c>schedule</c> hint for the company, then pushes. Call it after the save returned. The method reads no
        /// data and does no I/O, so a push never fails a committed change.
        /// </summary>
        /// <param name="companyId">The company of the employee.</param>
        /// <param name="change">The change, with the identity user read inside the transaction.</param>
        public void PushShiftChanged(Guid companyId, ShiftChange change)
        {
            if (change.IdentityUserId is { } identityUserId)
            {
                notifier.AddToUser(
                    identityUserId,
                    RealtimeNotificationKind.ShiftChanged,
                    new ShiftChangedData { EmployeeId = change.EmployeeId, Date = change.Date },
                    isActorSkipped: true);
            }

            notifier.AddHint(companyId, new RealtimeHint { Topic = RealtimeTopic.Schedule });
            notifier.Push();
        }
    }
}
