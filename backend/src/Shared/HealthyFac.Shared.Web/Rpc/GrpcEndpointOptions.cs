using System.Net;
using FluentValidation;

namespace HealthyFac.Shared.Web.Rpc;

/// <summary>
/// Configures the internal port that serves the gRPC services. A service answers gRPC calls on this port only, and
/// answers no HTTP request of its public API there.
/// </summary>
public sealed class GrpcEndpointOptions
{
    public const string SectionName = "Grpc";

    /// <summary>
    /// The port of the gRPC services. It must differ from the API port and the health port. The platform must not
    /// route it publicly. Kestrel listens on it with HTTP/2 without TLS.
    /// </summary>
    public required int Port { get; init; }
}

public sealed class GrpcEndpointOptionsValidator : AbstractValidator<GrpcEndpointOptions>
{
    public GrpcEndpointOptionsValidator()
    {
        RuleFor(options => options.Port)
            .GreaterThan(IPEndPoint.MinPort)
            .LessThanOrEqualTo(IPEndPoint.MaxPort);
    }
}
