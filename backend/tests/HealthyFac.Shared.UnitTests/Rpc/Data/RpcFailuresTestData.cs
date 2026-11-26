using Grpc.Core;

namespace HealthyFac.Shared.UnitTests.Rpc.Data;

public static class RpcFailuresTestData
{
    /// <summary>The case name, the status of the failure, and whether it counts as an outage.</summary>
    public static TheoryData<string, StatusCode, bool> Statuses => new()
    {
        { "Unavailable", StatusCode.Unavailable, true },
        { "DeadlineExceeded", StatusCode.DeadlineExceeded, true },
        { "Cancelled", StatusCode.Cancelled, false },
        { "Internal", StatusCode.Internal, false },
        { "PermissionDenied", StatusCode.PermissionDenied, false },
    };

    /// <summary>
    /// The case name, the token of the call, the status of the failure, and whether it counts as the cancel of
    /// the caller.
    /// </summary>
    public static TheoryData<string, CancellationToken, StatusCode, bool> Cancellations => new()
    {
        { "CallerCancelled", new CancellationToken(canceled: true), StatusCode.Cancelled, true },
        { "ServerCancelled", CancellationToken.None, StatusCode.Cancelled, false },
        { "CallerCancelledAndAnotherStatus", new CancellationToken(canceled: true), StatusCode.Unavailable, false },
    };
}
