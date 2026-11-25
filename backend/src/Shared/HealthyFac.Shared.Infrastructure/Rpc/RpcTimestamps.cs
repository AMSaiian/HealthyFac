using Google.Protobuf.WellKnownTypes;

namespace HealthyFac.Shared.Infrastructure.Rpc;

/// <summary>
/// Converts the times of a gRPC message. <see cref="Timestamp.FromDateTime"/> throws an
/// <see cref="ArgumentException"/> for a value that is not <see cref="DateTimeKind.Utc"/>, so the caller must give a
/// UTC value. The conversion never relabels the kind of a value, because a relabel moves the instant.
/// </summary>
public static class RpcTimestamps
{
    public static Timestamp ToTimestamp(DateTime value) => Timestamp.FromDateTime(value);

    public static Timestamp? ToTimestamp(DateTime? value) => value is null ? null : ToTimestamp(value.Value);
}
