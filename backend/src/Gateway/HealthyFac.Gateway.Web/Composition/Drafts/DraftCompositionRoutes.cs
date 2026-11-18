namespace HealthyFac.Gateway.Web.Composition.Drafts;

/// <summary>
/// Defines the public routes of the composed draft endpoints and the internal paths of the calls to the services.
/// The public routes have the same version segment as the YARP <c>core</c> route, and they are more specific than its
/// catch-all, so they win.
/// </summary>
public static class DraftCompositionRoutes
{
    public const string VersionParameter = "version";
    public const string MachineIdParameter = "machineId";
    public const string AttemptIdParameter = "attemptId";

    public const string CorePrefix = "/api/core/{" + VersionParameter + ":regex(^v[0-9]+([.][0-9]+)?$)}";

    public const string MachineDraftSearch = CorePrefix + "/machines/{" + MachineIdParameter + "}/drafts/search";
    public const string AttemptDraftSearch = CorePrefix + "/ingest-attempts/{" + AttemptIdParameter + "}/drafts/search";

    public const string AttemptDrafts = CorePrefix + "/ingest-attempts/{" + AttemptIdParameter + "}/drafts";
    public const string MachineDraftDismiss = CorePrefix + "/machines/{" + MachineIdParameter + "}/drafts/dismiss";
    public const string AttemptDraftDismiss = AttemptDrafts + "/dismiss";

    /// <summary>The Core path of the machine drafts of an ingest attempt, grouped by machine.</summary>
    public static string CoreAttemptDrafts(string version, string attemptId) =>
        $"/{version}/ingest-attempts/{Uri.EscapeDataString(attemptId)}/drafts";

    /// <summary>The Telemetry path of the definition drafts of an ingest attempt.</summary>
    public static string TelemetryAttemptDrafts(string version, string attemptId) =>
        $"/{version}/ingest-attempts/{Uri.EscapeDataString(attemptId)}/definition-drafts";

    /// <summary>The Core path that dismisses the machine drafts of a machine.</summary>
    public static string CoreMachineDismiss(string version, string machineId) =>
        $"/{version}/machines/{Uri.EscapeDataString(machineId)}/drafts/dismiss";

    /// <summary>The Telemetry path that dismisses the definition drafts of a machine.</summary>
    public static string TelemetryMachineDismiss(string version, string machineId) =>
        $"/{version}/machines/{Uri.EscapeDataString(machineId)}/definition-drafts/dismiss";

    /// <summary>The Core path that dismisses the machine drafts of an ingest attempt.</summary>
    public static string CoreAttemptDismiss(string version, string attemptId) =>
        $"/{version}/ingest-attempts/{Uri.EscapeDataString(attemptId)}/drafts/dismiss";

    /// <summary>The Telemetry path that dismisses the definition drafts of an ingest attempt.</summary>
    public static string TelemetryAttemptDismiss(string version, string attemptId) =>
        $"/{version}/ingest-attempts/{Uri.EscapeDataString(attemptId)}/definition-drafts/dismiss";

    /// <summary>The Core path of the machine draft search of a machine.</summary>
    public static string CoreMachineSearch(string version, string machineId) =>
        $"/{version}/machines/{Uri.EscapeDataString(machineId)}/drafts/search";

    /// <summary>The Core path of the machine draft search of an ingest attempt.</summary>
    public static string CoreAttemptSearch(string version, string attemptId) =>
        $"/{version}/ingest-attempts/{Uri.EscapeDataString(attemptId)}/drafts/search";

    /// <summary>The Telemetry path of the definition draft search of a machine.</summary>
    public static string TelemetryMachineSearch(string version, string machineId) =>
        $"/{version}/machines/{Uri.EscapeDataString(machineId)}/definition-drafts/search";

    /// <summary>The Telemetry path of the definition draft search of an ingest attempt.</summary>
    public static string TelemetryAttemptSearch(string version, string attemptId) =>
        $"/{version}/ingest-attempts/{Uri.EscapeDataString(attemptId)}/definition-drafts/search";

    /// <summary>The Core path of the machine name lookup.</summary>
    public static string CoreMachineNames(string version) => $"/{version}/machines/names";
}
