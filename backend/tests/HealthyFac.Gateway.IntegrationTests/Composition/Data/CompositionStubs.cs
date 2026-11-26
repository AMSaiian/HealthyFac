using HealthyFac.Gateway.IntegrationTests.Hosting;

namespace HealthyFac.Gateway.IntegrationTests.Composition.Data;

/// <summary>
/// Holds the stand-ins for Core and Telemetry of a composition test and the gateway that calls them. The defaults
/// answer every internal path that the composed endpoints call, so a test changes only what it checks.
/// </summary>
public sealed class CompositionStubs : IAsyncDisposable
{
    public const string AttemptId = "v1.attempt";
    public const string MachineId = "v1.machine";
    public const string PublicBase = "/api/core/v1";
    public const string OkJson = "{}";

    public static string PublicMachineDismiss => $"{PublicBase}/machines/{MachineId}/drafts/dismiss";

    public static string PublicAttemptDismiss => $"{PublicBase}/ingest-attempts/{AttemptId}/drafts/dismiss";

    public static string PublicAttemptDrafts => $"{PublicBase}/ingest-attempts/{AttemptId}/drafts";

    public static string PublicMachineDraftSearch => $"{PublicBase}/machines/{MachineId}/drafts/search";

    public static string PublicAttemptDraftSearch => $"{PublicBase}/ingest-attempts/{AttemptId}/drafts/search";

    public static string PublicMachineSearch => $"{PublicBase}/machines/search";

    public StubApiServer Core { get; } = new();

    public StubApiServer Telemetry { get; } = new();

    public static string CoreAttemptDrafts => $"/v1/ingest-attempts/{AttemptId}/drafts";

    public static string TelemetryAttemptDrafts => $"/v1/ingest-attempts/{AttemptId}/definition-drafts";

    public static string CoreMachineDismiss => $"/v1/machines/{MachineId}/drafts/dismiss";

    public static string TelemetryMachineDismiss => $"/v1/machines/{MachineId}/definition-drafts/dismiss";

    public static string CoreAttemptDismiss => $"/v1/ingest-attempts/{AttemptId}/drafts/dismiss";

    public static string TelemetryAttemptDismiss => $"/v1/ingest-attempts/{AttemptId}/definition-drafts/dismiss";

    public const string CoreMachineSearch = "/v1/machines/search";
    public const string TelemetryPendingCounts = "/v1/definition-drafts/pending-counts";

    public async Task StartAsync()
    {
        await Core.StartAsync();
        await Telemetry.StartAsync();
        ServeDefaults();
    }

    public GatewayFactory CreateGateway(
        GatewayEnvironment environment,
        IReadOnlyDictionary<string, string>? extraSettings = null
    ) =>
        environment.CreateGateway(
            Core.Address,
            settings: new Dictionary<string, string>(extraSettings ?? new Dictionary<string, string>())
            {
                [GatewaySettingKeys.TelemetryAddress] = Telemetry.Address,
                [GatewaySettingKeys.TelemetryHealthAddress] = Telemetry.Address,
            });

    public async ValueTask DisposeAsync()
    {
        await Core.DisposeAsync();
        await Telemetry.DisposeAsync();
    }

    private void ServeDefaults()
    {
        DraftSourceStub.Serve(Core, DraftSearchCompositionTestData.CoreSearchPath, []);
        DraftSourceStub.Serve(Telemetry, DraftSearchCompositionTestData.TelemetrySearchPath, []);

        Core.Respond(
            HttpMethod.Post,
            DraftSearchCompositionTestData.NamesPath,
            status: 200,
            json: "{\"machines\":[],\"machineDrafts\":[]}");

        Core.Respond(HttpMethod.Get, CoreAttemptDrafts, status: 200, json: "{\"groups\":[]}");

        Telemetry.Respond(
            HttpMethod.Get, TelemetryAttemptDrafts, status: 200, json: "{\"metrics\":[],\"statuses\":[]}");

        Core.Respond(HttpMethod.Post, CoreMachineDismiss, status: 200, json: "{\"dismissedCount\":0}");
        Telemetry.Respond(HttpMethod.Post, TelemetryMachineDismiss, status: 200, json: "{\"dismissedCount\":0}");
        Core.Respond(HttpMethod.Post, CoreAttemptDismiss, status: 200, json: "{\"dismissedCount\":0}");
        Telemetry.Respond(HttpMethod.Post, TelemetryAttemptDismiss, status: 200, json: "{\"dismissedCount\":0}");
        Core.Respond(HttpMethod.Post, CoreMachineSearch, status: 200, json: "{\"items\":[],\"totalCount\":0}");
        Telemetry.Respond(HttpMethod.Get, TelemetryPendingCounts, status: 200, json: "{\"counts\":[]}");
    }
}
