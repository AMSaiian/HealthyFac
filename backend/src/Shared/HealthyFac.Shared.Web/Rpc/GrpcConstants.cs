namespace HealthyFac.Shared.Web.Rpc;

/// <summary>
/// Defines transport limits and shared texts of the gRPC server.
/// </summary>
public static class GrpcConstants
{
    public const int MaxReceiveMessageSizeBytes = 4 * 1024 * 1024;

    /// <summary>The content type prefix of every gRPC call.</summary>
    public const string ContentType = "application/grpc";

    public const string UnexpectedErrorDetail = "An unexpected error occurred.";
}
