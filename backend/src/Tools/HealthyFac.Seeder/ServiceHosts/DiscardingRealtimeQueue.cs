using System.Diagnostics;
using HealthyFac.Shared.Application.BackgroundQueues;
using HealthyFac.Shared.Application.Realtime;

namespace HealthyFac.Seeder.ServiceHosts;

/// <summary>
/// Takes the place of the realtime push queue. No client listens while the seed job runs, so the pushes are dropped.
/// </summary>
internal sealed class DiscardingRealtimeQueue : IBackgroundQueue<RealtimePush>
{
    public void Enqueue(RealtimePush item, ActivityContext parentContext)
    {
    }
}
