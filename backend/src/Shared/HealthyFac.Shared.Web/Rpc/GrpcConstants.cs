namespace HealthyFac.Shared.Web.Rpc;

/// <summary>
/// Defines retry policy defaults and transport limits for gRPC servers and client channels.
/// </summary>
public static class GrpcConstants
{
    public const int MaxReceiveMessageSizeBytes = 4 * 1024 * 1024;

    public static class Retry
    {
        public const int MaxAttempts = 5;
        public const int InitialBackoffSeconds = 1;
        public const int MaxBackoffSeconds = 10;
        public const double BackoffMultiplier = 2.0;
    }
}
