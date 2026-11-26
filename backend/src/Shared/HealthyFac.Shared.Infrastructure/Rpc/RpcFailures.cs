using Grpc.Core;

namespace HealthyFac.Shared.Infrastructure.Rpc;

/// <summary>
/// Classifies the failures of an outgoing gRPC call, so every caller reads the same rules.
/// </summary>
public static class RpcFailures
{
    /// <summary>
    /// Tells whether the call failed because the service did not answer (the status <c>Unavailable</c> or
    /// <c>DeadlineExceeded</c>).
    /// </summary>
    /// <param name="exception">The gRPC failure.</param>
    /// <returns><see langword="true"/> for an outage.</returns>
    public static bool IsOutage(RpcException exception) =>
        exception.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded;

    /// <summary>
    /// Tells whether the call ended with <c>Cancelled</c> because the caller cancelled it. A cancel that the server
    /// sent is a failure, not a caller cancel.
    /// </summary>
    /// <param name="exception">The gRPC failure.</param>
    /// <param name="cancellationToken">The token of the caller.</param>
    /// <returns><see langword="true"/> when the caller cancelled the call.</returns>
    public static bool IsCancelledByCaller(RpcException exception, CancellationToken cancellationToken) =>
        exception.StatusCode is StatusCode.Cancelled && cancellationToken.IsCancellationRequested;
}
