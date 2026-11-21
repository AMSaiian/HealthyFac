using HealthyFac.Shared.Application.Realtime;

namespace HealthyFac.Core.Application.Realtime;

/// <summary>
/// Adds the real-time events of a machine change and pushes them.
/// </summary>
public static class MachineChangeNotifications
{
    extension(IRealtimeNotifier notifier)
    {
        /// <summary>
        /// Adds the <c>machines</c> hint and pushes. Call it after the save returned. The method reads no data and does
        /// no I/O, so a push never fails a committed change.
        /// </summary>
        /// <param name="companyId">The company of the machine.</param>
        public void PushMachinesHint(Guid companyId)
        {
            notifier.AddHint(companyId, new RealtimeHint { Topic = RealtimeTopic.Machines });
            notifier.Push();
        }
    }
}
