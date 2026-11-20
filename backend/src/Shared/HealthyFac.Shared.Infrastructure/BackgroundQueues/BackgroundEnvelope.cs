using System.Diagnostics;

namespace HealthyFac.Shared.Infrastructure.BackgroundQueues;

internal sealed class BackgroundEnvelope<TItem>
{
    public required TItem Item { get; init; }

    public required ActivityContext ParentContext { get; init; }
}
