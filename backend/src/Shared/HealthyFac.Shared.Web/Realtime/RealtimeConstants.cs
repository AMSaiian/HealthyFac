namespace HealthyFac.Shared.Web.Realtime;

public static class RealtimeConstants
{
    public const string HubPath = "/api/realtime/v1/hub";

    /// <summary>
    /// The query key of the access token. A browser WebSocket cannot send an <c>Authorization</c> header.
    /// </summary>
    public const string AccessTokenQueryKey = "access_token";
}
