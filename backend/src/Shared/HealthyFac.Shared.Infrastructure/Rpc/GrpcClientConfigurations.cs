using Grpc.Core;
using Grpc.Net.Client.Configuration;

namespace HealthyFac.Shared.Infrastructure.Rpc;

/// <summary>
/// Builds the channel configuration of resilient gRPC clients.
/// </summary>
public static class GrpcClientConfigurations
{
    /// <summary>
    /// Builds the retry service config applied to resilient gRPC client channels.
    /// Retries only status codes that are safe to repeat, so a failed write is never duplicated.
    /// </summary>
    /// <returns>The <see cref="ServiceConfig"/> carrying the shared retry policy.</returns>
    public static ServiceConfig CreateResilientServiceConfig()
        => new()
        {
            MethodConfigs =
            {
                new MethodConfig
                {
                    Names = { MethodName.Default },
                    RetryPolicy = new RetryPolicy
                    {
                        MaxAttempts = GrpcClientConstants.Retry.MaxAttempts,
                        InitialBackoff = TimeSpan.FromSeconds(GrpcClientConstants.Retry.InitialBackoffSeconds),
                        MaxBackoff = TimeSpan.FromSeconds(GrpcClientConstants.Retry.MaxBackoffSeconds),
                        BackoffMultiplier = GrpcClientConstants.Retry.BackoffMultiplier,
                        RetryableStatusCodes =
                        {
                            StatusCode.Unavailable,
                            StatusCode.ResourceExhausted,
                            StatusCode.Aborted,
                        },
                    },
                },
            },
        };
}
