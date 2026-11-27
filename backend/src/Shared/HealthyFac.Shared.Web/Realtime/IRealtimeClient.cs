using HealthyFac.Shared.Application.Realtime;

namespace HealthyFac.Shared.Web.Realtime;

/// <summary>
/// The methods that the web app implements on a hub connection. The hub has no method that a client calls.
/// </summary>
public interface IRealtimeClient
{
    Task Notification(RealtimeNotification notification);

    Task Hint(RealtimeHint hint);

    Task ChatTurnUpdated(ChatTurnUpdate update);

    Task ChatTurnEnded(ChatTurnEnd end);

    Task SessionEnded();
}
