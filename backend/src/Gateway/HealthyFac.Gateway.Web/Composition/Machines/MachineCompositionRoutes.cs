using HealthyFac.Gateway.Web.Composition.Drafts;

namespace HealthyFac.Gateway.Web.Composition.Machines;

/// <summary>
/// Defines the public route of the composed machine search and the internal paths of its calls.
/// </summary>
public static class MachineCompositionRoutes
{
    public const string MachineSearch = DraftCompositionRoutes.CorePrefix + "/machines/search";

    /// <summary>The Core path of the machine search.</summary>
    public static string CoreMachineSearch(string version) => $"/{version}/machines/search";

    /// <summary>The Telemetry path of the pending definition draft counts of the machines.</summary>
    public static string TelemetryPendingCounts(string version) => $"/{version}/definition-drafts/pending-counts";
}
