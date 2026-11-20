namespace HealthyFac.Shared.Web.Realtime;

public static class RealtimeConstants
{
    public const string HubPath = "/api/realtime/v1/hub";

    /// <summary>
    /// The query key of the access token. A browser WebSocket cannot send an <c>Authorization</c> header.
    /// </summary>
    public const string AccessTokenQueryKey = "access_token";

    public static class Push
    {
        /// <summary>The key of the Polly pipeline that retries a push.</summary>
        public const string PipelineKey = "realtime-push";

        public const int MaxRetryCount = 3;

        public static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(milliseconds: 200);
    }
}
