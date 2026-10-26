using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Health;
using Yarp.ReverseProxy.Model;

namespace HealthyFac.Gateway.Web.Proxy;

/// <summary>
/// Excludes destinations that the active or the passive health checks mark unhealthy. A destination with an
/// unknown state stays available, so the gateway can serve traffic before the first health check ends.
/// </summary>
/// <remarks>
/// The YARP default policy falls back to all destinations when none is healthy. Then a request reaches a dead
/// service and fails slowly. This policy leaves no destination, so the proxy pipeline answers at once and the
/// client gets <c>503</c>.
/// </remarks>
internal sealed class HealthyOnlyDestinationsPolicy : IAvailableDestinationsPolicy
{
    public const string PolicyName = "HealthyOnly";

    public string Name => PolicyName;

    public IReadOnlyList<DestinationState> GetAvailalableDestinations(
        ClusterConfig config,
        IReadOnlyList<DestinationState> allDestinations
    ) => allDestinations
        .Where(destination => destination.Health.Active != DestinationHealth.Unhealthy
            && destination.Health.Passive != DestinationHealth.Unhealthy)
        .ToList();
}
