namespace HealthyFac.Shared.Infrastructure.Rpc;

/// <summary>
/// Defines the retry policy defaults of gRPC client channels.
/// </summary>
public static class GrpcClientConstants
{
    public static class Retry
    {
        public const int MaxAttempts = 5;
        public const int InitialBackoffSeconds = 1;
        public const int MaxBackoffSeconds = 10;
        public const double BackoffMultiplier = 2.0;
    }
}
