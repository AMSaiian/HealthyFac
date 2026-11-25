namespace HealthyFac.Gateway.IntegrationTests.Composition.Data;

public static class CompositionFailureTestData
{
    public const string ForbiddenJson = """{"title":"Forbidden","status":403}""";
    public const string NotFoundJson = """{"title":"Not Found","status":404}""";

    private const string SearchBody = """{"limit":5}""";
    private const string MachineSearchBody = """{"offset":0,"limit":25,"filter":{}}""";

    /// <summary>The case name, the method, the public path, and the body of each composed endpoint.</summary>
    public static TheoryData<string, string, string, string?> Endpoints => new()
    {
        { "machine draft search", "POST", CompositionStubs.PublicMachineDraftSearch, SearchBody },
        { "attempt draft search", "POST", CompositionStubs.PublicAttemptDraftSearch, SearchBody },
        { "attempt groups", "GET", CompositionStubs.PublicAttemptDrafts, null },
        { "chat draft search", "POST", CompositionStubs.PublicChatDraftSearch, SearchBody },
        { "chat groups", "GET", CompositionStubs.PublicChatDrafts, null },
        { "chat dismiss", "POST", CompositionStubs.PublicChatDismiss, null },
        { "machine dismiss", "POST", CompositionStubs.PublicMachineDismiss, null },
        { "attempt dismiss", "POST", CompositionStubs.PublicAttemptDismiss, null },
        { "machine search", "POST", CompositionStubs.PublicMachineSearch, MachineSearchBody },
    };
}
